#!/usr/bin/env python3
"""Gate release publication on immutable, downloadable SBOM assets."""

import argparse
import io
import json
import os
from pathlib import Path
import subprocess
import urllib.error
import urllib.parse
import urllib.request
import zipfile

from sbom import package_metadata, package_set, require, verify


def request(url, data=None, method=None, github=True, content_type="application/json",
            accept="application/vnd.github+json"):
    headers = {"User-Agent": "SwiftDispatch-release"}
    if github:
        headers.update({"Authorization": f"Bearer {os.environ['GH_TOKEN']}",
                        "Accept": accept, "X-GitHub-Api-Version": "2022-11-28"})
    if data is not None:
        headers["Content-Type"] = content_type
        if not isinstance(data, bytes):
            data = json.dumps(data).encode()
    try:
        with urllib.request.urlopen(urllib.request.Request(url, data, headers, method=method), timeout=120) as response:
            return response.read()
    except urllib.error.HTTPError as error:
        if error.code == 404:
            return None
        raise


def api(path, **kwargs):
    result = request(f"https://api.github.com/repos/{os.environ['GITHUB_REPOSITORY']}/{path}", **kwargs)
    return json.loads(result) if result is not None else None


def restore(directory):
    # A rerun of the same Actions run reuses bytes saved before any publication.
    artifacts = api(f"actions/runs/{os.environ['GITHUB_RUN_ID']}/artifacts?per_page=100")
    matches = [a for a in artifacts["artifacts"] if a["name"] == "release-bundle"]
    require(len(matches) <= 1, "Ambiguous original release bundle")
    if matches:
        artifact = matches[0]
        require(not artifact["expired"], "Original release outputs expired; recover and review them manually")
        data = request(artifact["archive_download_url"])
        from sbom import extract
        extract(io.BytesIO(data), directory)
        verify(directory)
    else:
        # Never regenerate SBOMs for a draft left behind by a previous run.
        release = api(f"releases/tags/{urllib.parse.quote(os.environ['GITHUB_REF_NAME'], safe='')}")
        require(release is None, "Release already exists without this run's original bundle; manual recovery required")
    with open(os.environ["GITHUB_OUTPUT"], "a") as output:
        output.write(f"reused={'true' if matches else 'false'}\n")


def release_for_tag():
    tag = os.environ["GITHUB_REF_NAME"]
    release = api(f"releases/tags/{urllib.parse.quote(tag, safe='')}")
    require(release is not None, "Missing draft release")
    require(release["target_commitish"] == os.environ["GITHUB_SHA"], "Release commit differs from this run")
    return release


def check_assets(directory, release):
    assets = api(f"releases/{release['id']}/assets?per_page=100")
    expected = {p.name: p for p in (directory / "sbom").glob("*.json")}
    actual = {a["name"]: a for a in assets if a["name"].endswith(".sbom.spdx.json")}
    require(set(actual) == set(expected), "Release SBOM assets do not match final packages")
    for name, path in expected.items():
        require(request(actual[name]["url"], accept="application/octet-stream") == path.read_bytes(),
                f"Downloaded release asset differs: {name}")


def existing_nuget(package):
    name, version, _ = package_metadata(package)
    url = f"https://api.nuget.org/v3-flatcontainer/{name.lower()}/{version.lower()}/{name.lower()}.{version.lower()}.nupkg"
    data = request(url, github=False)
    if data is None:
        return False
    # NuGet adds its repository signature. Every other entry must be identical.
    def contents(source):
        with zipfile.ZipFile(source) as archive:
            return {n: archive.read(n) for n in archive.namelist()
                    if not n.endswith("/") and n != ".signature.p7s"}
    require(contents(package) == contents(io.BytesIO(data)),
            f"NuGet already contains different bytes for {name} {version}; manual review required")
    return True


def stage(directory):
    verify(directory)
    # Check all versions before creating a draft or uploading any assets.
    for package in package_set(directory):
        existing_nuget(package)
    tag = os.environ["GITHUB_REF_NAME"]
    release = api(f"releases/tags/{urllib.parse.quote(tag, safe='')}")
    if release is None:
        links = "\n".join(f"- {p.name.removesuffix('.sbom.spdx.json')}: "
                          f"[Download SBOM (SPDX JSON)](https://github.com/{os.environ['GITHUB_REPOSITORY']}"
                          f"/releases/download/{tag}/{p.name})" for p in sorted((directory / "sbom").glob("*.json")))
        release = api("releases", data={"tag_name": tag, "target_commitish": os.environ["GITHUB_SHA"],
                      "name": tag, "draft": True, "prerelease": "-" in tag,
                      "generate_release_notes": True, "body": f"## Software Bill of Materials\n\n{links}"})
    require(release["target_commitish"] == os.environ["GITHUB_SHA"], "Release commit mismatch")
    assets = api(f"releases/{release['id']}/assets?per_page=100")
    by_name = {a["name"]: a for a in assets}
    for path in sorted((directory / "sbom").glob("*.json")):
        if path.name in by_name:
            require(request(by_name[path.name]["url"], accept="application/octet-stream") == path.read_bytes(),
                    f"Refusing to replace existing SBOM: {path.name}")
        else:
            require(release["draft"], "Cannot add an asset to a published release")
            url = release["upload_url"].split("{")[0] + "?" + urllib.parse.urlencode({"name": path.name})
            request(url, path.read_bytes(), content_type="application/json")
    check_assets(directory, release)


def publish_packages(directory):
    verify(directory)
    check_assets(directory, release_for_tag())
    packages = package_set(directory)
    existing = {p: existing_nuget(p) for p in packages}
    for package in packages:
        if not existing[package]:
            subprocess.run(["dotnet", "nuget", "push", str(package), "--api-key", os.environ["NUGET_API_KEY"],
                            "--source", "https://api.nuget.org/v3/index.json"], check=True)


def publish_release(directory):
    verify(directory)
    release = release_for_tag()
    check_assets(directory, release)
    if release["draft"]:
        api(f"releases/{release['id']}", data={"draft": False}, method="PATCH")
    for asset in api(f"releases/{release['id']}/assets?per_page=100"):
        if asset["name"].endswith(".sbom.spdx.json"):
            require(request(asset["browser_download_url"], github=False) ==
                    (directory / "sbom" / asset["name"]).read_bytes(),
                    f"Public SBOM download differs: {asset['name']}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("restore", "stage", "packages", "publish"))
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    {"restore": restore, "stage": stage, "packages": publish_packages,
     "publish": publish_release}[args.action](args.directory.resolve())
