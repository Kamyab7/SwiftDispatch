#!/usr/bin/env python3
"""Generate and verify one SPDX 2.2 SBOM per final NuGet package."""

import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
PROJECTS = {name: ROOT / "src" / name / f"{name}.csproj" for name in (
    "SwiftDispatch", "SwiftDispatch.Contracts", "SwiftDispatch.FluentValidation",
    "SwiftDispatch.EntityFrameworkCore",
)}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def package_metadata(package):
    with zipfile.ZipFile(package) as archive:
        specs = [n for n in archive.namelist() if n.endswith(".nuspec") and "/" not in n]
        require(len(specs) == 1, f"Expected one root nuspec: {package}")
        spec = ET.fromstring(archive.read(specs[0]))
        # NuGet has used several nuspec namespace versions.
        for element in spec.iter():
            element.tag = element.tag.rsplit("}", 1)[-1]
        metadata = spec.find("metadata")
        name, version = metadata.findtext("id"), metadata.findtext("version")
        require(name in PROJECTS, f"Unknown shipping package: {name}")
        require(version and re.fullmatch(r"[0-9A-Za-z.+-]+", version), "Invalid package version")
        return name, version, metadata


def package_set(directory):
    packages = sorted(directory.glob("*.nupkg"))  # .snupkg is intentionally separate.
    identities = [package_metadata(p)[:2] for p in packages]
    require(len(identities) == len(PROJECTS) and {n for n, _ in identities} == set(PROJECTS),
            "Expected exactly one final nupkg for each shipping project")
    require(len({v for _, v in identities}) == 1, "Shipping package versions must agree")
    return packages


def extract(package, destination):
    with zipfile.ZipFile(package) as archive:
        for entry in archive.infolist():
            path = PurePosixPath(entry.filename)
            require(not path.is_absolute() and ".." not in path.parts and "\\" not in entry.filename,
                    f"Unsafe package path: {entry.filename}")
        archive.extractall(destination)


def stage_inputs(project, destination, visited=None):
    """Copy only each referenced project's source and original restore graph."""
    visited = visited if visited is not None else set()
    project = project.resolve()
    if project in visited:
        return
    visited.add(project)
    require(project.is_relative_to(ROOT / "src"), f"Non-shipping project reference: {project}")
    assets = project.parent / "obj/project.assets.json"
    require(assets.is_file(), f"Missing release restore graph: {assets}")
    target = destination / project.parent.relative_to(ROOT)
    shutil.copytree(project.parent, target, ignore=shutil.ignore_patterns("bin", "obj"))
    (target / "obj").mkdir()
    shutil.copy2(assets, target / "obj/project.assets.json")
    for reference in ET.parse(project).iter("ProjectReference"):
        stage_inputs(project.parent / reference.attrib["Include"], destination, visited)


def check_manifest(package_dir, manifest, name, version, inputs=None, metadata=None):
    document = json.loads(manifest.read_text())
    require(document.get("spdxVersion") == "SPDX-2.2", "Expected SPDX-2.2")
    packages = document.get("packages", [])
    described = {r["relatedSpdxElement"] for r in document.get("relationships", [])
                 if r["spdxElementId"] == document["SPDXID"] and r["relationshipType"] == "DESCRIBES"}
    roots = [p for p in packages if p["SPDXID"] in described]
    require(len(roots) == 1 and roots[0]["name"] == name and roots[0]["versionInfo"] == version,
            f"Wrong SBOM root: {name} {version}")
    actual = {p.relative_to(package_dir).as_posix(): p for p in package_dir.rglob("*")
              if p.is_file() and "_manifest" not in p.relative_to(package_dir).parts}
    entries = document.get("files", [])
    listed = {f["fileName"].removeprefix("./"): f for f in entries}
    require(len(entries) == len(listed) and set(listed) == set(actual),
            f"SBOM file inventory differs from {name} (including lib/analyzer DLLs)")
    for path, file in actual.items():
        hashes = {h["algorithm"]: h["checksumValue"].lower() for h in listed[path]["checksums"]}
        require(hashes.get("SHA256") == hashlib.sha256(file.read_bytes()).hexdigest(),
                f"Missing or incorrect SHA256: {path}")
    detected = {(p["name"].lower(), p.get("versionInfo", "").lower()) for p in packages}
    expected = set()
    targets = {}
    if inputs is not None:
        graphs = sorted(inputs.rglob("project.assets.json"))
        require(graphs, f"Original restore graphs missing for {name}")
        for graph in graphs:
            assets = json.loads(graph.read_text())
            targets[graph.relative_to(inputs).as_posix()] = {
                framework: sorted(libraries) for framework, libraries in assets.get("targets", {}).items()
            }
            for key, library in assets["libraries"].items():
                if library["type"] == "package" or (
                        library["type"] == "project" and key.rsplit("/", 1)[0] in PROJECTS):
                    dependency, dependency_version = key.rsplit("/", 1)
                    expected.add((dependency.lower(), dependency_version.lower()))
        require(expected <= detected, f"Restore dependencies missing from {name}: {sorted(expected - detected)}")
        for dependency in metadata.iter("dependency"):
            require(any(n == dependency.attrib["id"].lower() for n, _ in detected),
                    f"Nuspec dependency missing from {name}: {dependency.attrib['id']}")
    return {"package": name, "version": version, "files": len(actual),
            "restorePackages": sorted(expected), "detectedPackages": sorted(detected),
            "restoreTargets": targets, "nuspecDependencyGroups": [
                {"framework": group.get("targetFramework", "all"),
                 "dependencies": [dict(d.attrib) for d in group.findall("dependency")]}
                for group in metadata.iter("group")
            ] if metadata is not None else []}


def generate(directory):
    packages = package_set(directory)
    output = directory / "sbom"
    output.mkdir(exist_ok=False)
    reports = directory / "validation"
    reports.mkdir(exist_ok=False)
    for package in packages:
        name, version, metadata = package_metadata(package)
        with tempfile.TemporaryDirectory(prefix="swiftdispatch-sbom-") as work:
            work = Path(work)
            payload, inputs = work / "package", work / "inputs"
            extract(package, payload)
            stage_inputs(PROJECTS[name], inputs)
            # Component Detection excludes project references from assets files.
            # Feed it the referenced shipping packages' actual nuspec identities.
            for referenced_package in packages:
                referenced_name, _, _ = package_metadata(referenced_package)
                referenced_dir = inputs / "src" / referenced_name
                if referenced_name != name and referenced_dir.is_dir():
                    with zipfile.ZipFile(referenced_package) as archive:
                        spec = next(n for n in archive.namelist() if n.endswith(".nuspec") and "/" not in n)
                        (referenced_dir / "obj" / spec).write_bytes(archive.read(spec))
            for props in ROOT.glob("Directory.*.props"):
                shutil.copy2(props, inputs / props.name)
            command = ["dotnet", "tool", "run", "sbom-tool", "--"]
            subprocess.run(command + ["generate", "-b", str(payload), "-bc", str(inputs),
                           "-pn", name, "-pv", version, "-ps", "Kamyab Valipour",
                           "-nsb", "https://github.com/Kamyab7/SwiftDispatch/sbom", "-mi", "SPDX:2.2"],
                           cwd=ROOT, check=True)
            subprocess.run(command + ["validate", "-b", str(payload), "-mi", "SPDX:2.2",
                           "-o", str(reports / f"{name}.{version}.validation.json")], cwd=ROOT, check=True)
            manifest = payload / "_manifest/spdx_2.2/manifest.spdx.json"
            coverage = check_manifest(payload, manifest, name, version, inputs, metadata)
            (reports / f"{name}.{version}.coverage.json").write_text(json.dumps(coverage, indent=2) + "\n")
            # Preserve the original graphs for dependency review and retry evidence.
            shutil.copytree(inputs, reports / f"{name}.{version}.inputs")
            shutil.copy2(manifest, output / f"{name}.{version}.sbom.spdx.json")
    verify(directory)


def verify(directory):
    expected = set()
    for package in package_set(directory):
        name, version, metadata = package_metadata(package)
        filename = f"{name}.{version}.sbom.spdx.json"
        expected.add(filename)
        report = directory / "validation" / f"{name}.{version}.validation.json"
        validation = json.loads(report.read_text())
        require(validation.get("Result") == "Success" and
                validation.get("ValidationErrors", {}).get("Count") == 0,
                f"Microsoft SBOM validation did not succeed for {name}")
        with tempfile.TemporaryDirectory(prefix="swiftdispatch-verify-") as work:
            payload = Path(work)
            extract(package, payload)
            inputs = directory / "validation" / f"{name}.{version}.inputs"
            require(inputs.is_dir(), f"Original build inputs missing for {name}")
            check_manifest(payload, directory / "sbom" / filename, name, version, inputs, metadata)
    require({p.name for p in (directory / "sbom").iterdir()} == expected,
            "Expected exactly one versioned SBOM asset per nupkg")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("generate", "verify"))
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    {"generate": generate, "verify": verify}[args.action](args.directory.resolve())
