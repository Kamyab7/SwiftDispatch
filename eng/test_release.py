"""Publication gates are tested offline; actual SBOM generation runs in verify.sh."""

import hashlib
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import release
import sbom


class ReleaseTests(unittest.TestCase):
    def setUp(self):
        self.work = tempfile.TemporaryDirectory()
        self.addCleanup(self.work.cleanup)
        self.directory = Path(self.work.name)
        self.version = "3.0.0-rc.1"
        (self.directory / "sbom").mkdir()
        for name in sbom.PROJECTS:
            payload = {f"lib/net11.0/{name}.dll": b"library", f"{name}.nuspec":
                       f'<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata>'
                       f'<id>{name}</id><version>{self.version}</version></metadata></package>'.encode()}
            if name == "SwiftDispatch":
                payload["analyzers/dotnet/cs/SwiftDispatch.SourceGenerator.dll"] = b"generator"
            with zipfile.ZipFile(self.directory / f"{name}.{self.version}.nupkg", "w") as archive:
                for filename, data in payload.items():
                    archive.writestr(filename, data)
            inputs = self.directory / "validation" / f"{name}.{self.version}.inputs"
            inputs.mkdir(parents=True)
            (inputs / "project.assets.json").write_text('{"libraries": {}}')
            (self.directory / "validation" / f"{name}.{self.version}.validation.json").write_text(
                '{"Result": "Success", "ValidationErrors": {"Count": 0}}')
            document = {"spdxVersion": "SPDX-2.2", "SPDXID": "SPDXRef-DOCUMENT",
                        "packages": [{"SPDXID": "SPDXRef-root", "name": name, "versionInfo": self.version}],
                        "relationships": [{"spdxElementId": "SPDXRef-DOCUMENT", "relationshipType": "DESCRIBES",
                                           "relatedSpdxElement": "SPDXRef-root"}],
                        "files": [{"fileName": f"./{filename}", "checksums": [{"algorithm": "SHA256",
                                   "checksumValue": hashlib.sha256(data).hexdigest()}]} for filename, data in payload.items()]}
            self.manifest(name).write_text(json.dumps(document))
        self.environment = patch.dict(os.environ, {"GITHUB_REPOSITORY": "Kamyab7/SwiftDispatch",
                                      "GITHUB_REF_NAME": "v" + self.version, "GITHUB_SHA": "abc123",
                                      "GITHUB_RUN_ID": "42", "GITHUB_OUTPUT": str(self.directory / "output")})
        self.environment.start()
        self.addCleanup(self.environment.stop)

    def manifest(self, name="SwiftDispatch"):
        return self.directory / "sbom" / f"{name}.{self.version}.sbom.spdx.json"

    def mutate(self, callback):
        document = json.loads(self.manifest().read_text())
        callback(document)
        self.manifest().write_text(json.dumps(document))

    def test_prerelease_and_symbol_packages(self):
        (self.directory / "ignored.snupkg").write_bytes(b"symbols")
        sbom.verify(self.directory)

    def test_missing_manifest_blocks_publication(self):
        self.manifest().unlink()
        with patch.object(release, "api") as api:
            with self.assertRaises(FileNotFoundError):
                release.stage(self.directory)
            api.assert_not_called()

    def test_missing_analyzer_is_rejected(self):
        self.mutate(lambda d: d.update(files=[f for f in d["files"] if "analyzers/" not in f["fileName"]]))
        with self.assertRaisesRegex(ValueError, "inventory"):
            sbom.verify(self.directory)

    def test_wrong_root_rejected(self):
        self.mutate(lambda d: d["packages"][0].update(versionInfo="3.0.0"))
        with self.assertRaisesRegex(ValueError, "root"):
            sbom.verify(self.directory)

    def test_wrong_spdx_format_rejected(self):
        self.mutate(lambda d: d.update(spdxVersion="SPDX-3.0"))
        with self.assertRaisesRegex(ValueError, "SPDX-2.2"):
            sbom.verify(self.directory)

    def test_wrong_hash_rejected(self):
        self.mutate(lambda d: d["files"][0]["checksums"][0].update(checksumValue="0" * 64))
        with self.assertRaisesRegex(ValueError, "SHA256"):
            sbom.verify(self.directory)

    def test_missing_restore_dependency_rejected(self):
        graph = self.directory / "validation" / f"SwiftDispatch.{self.version}.inputs/project.assets.json"
        graph.write_text('{"libraries": {"ThirdParty/1.2.3": {"type": "package"}}}')
        with self.assertRaisesRegex(ValueError, "Restore dependencies missing"):
            sbom.verify(self.directory)

    def test_failed_original_validation_blocks_retry(self):
        report = self.directory / "validation" / f"SwiftDispatch.{self.version}.validation.json"
        report.write_text('{"Result": "Failure", "ValidationErrors": {"Count": 1}}')
        with self.assertRaisesRegex(ValueError, "validation did not succeed"):
            sbom.verify(self.directory)

    def test_partial_nuget_publication_reuses_original_packages(self):
        with patch.object(release, "release_for_tag", return_value={}), \
                patch.object(release, "check_assets"), \
                patch.object(release, "existing_nuget", side_effect=[True, False, True, True]), \
                patch.dict(os.environ, {"NUGET_API_KEY": "test-key"}), \
                patch.object(release.subprocess, "run") as push:
            release.publish_packages(self.directory)
            self.assertEqual(push.call_count, 1)
            self.assertNotIn("--skip-duplicate", push.call_args.args[0])

    def test_missing_release_asset_blocks_all_nuget_pushes(self):
        with patch.object(release, "release_for_tag", return_value={"id": 1}), \
                patch.object(release, "api", return_value=[]), \
                patch.object(release.subprocess, "run") as push:
            with self.assertRaisesRegex(ValueError, "assets do not match"):
                release.publish_packages(self.directory)
            push.assert_not_called()

    def test_signed_nuget_payload_must_match_original(self):
        package = sbom.package_set(self.directory)[0]
        signed = io.BytesIO()
        with zipfile.ZipFile(package) as source, zipfile.ZipFile(signed, "w") as destination:
            for filename in source.namelist():
                destination.writestr(filename, source.read(filename))
            destination.writestr(".signature.p7s", b"nuget signature")
        with patch.object(release, "request", return_value=signed.getvalue()):
            self.assertTrue(release.existing_nuget(package))
        with zipfile.ZipFile(signed, "a") as destination:
            destination.writestr("extra.dll", b"different publication")
        with patch.object(release, "request", return_value=signed.getvalue()):
            with self.assertRaisesRegex(ValueError, "different bytes"):
                release.existing_nuget(package)

    def test_existing_asset_is_never_replaced(self):
        draft = {"id": 1, "target_commitish": "abc123", "draft": True}
        assets = [{"name": self.manifest().name, "url": "https://api.github.com/asset"}]
        with patch.object(release, "existing_nuget", return_value=False), \
                patch.object(release, "api", side_effect=[draft, assets]), \
                patch.object(release, "request", return_value=b"different") as download:
            with self.assertRaisesRegex(ValueError, "Refusing to replace"):
                release.stage(self.directory)
            self.assertEqual(download.call_count, 1)

    def test_retry_without_original_bundle_fails_for_existing_release(self):
        with patch.object(release, "api", side_effect=[{"artifacts": []}, {"id": 1}]):
            with self.assertRaisesRegex(ValueError, "manual recovery"):
                release.restore(self.directory)

    def test_retry_restores_byte_identical_original_bundle(self):
        bundle = io.BytesIO()
        with zipfile.ZipFile(bundle, "w") as archive:
            for path in self.directory.rglob("*"):
                if path.is_file():
                    archive.writestr(path.relative_to(self.directory).as_posix(), path.read_bytes())
        with tempfile.TemporaryDirectory() as destination, \
                patch.object(release, "api", return_value={"artifacts": [
                    {"name": "release-bundle", "expired": False, "archive_download_url": "https://example.test/bundle"}]}), \
                patch.object(release, "request", return_value=bundle.getvalue()):
            release.restore(Path(destination))
            sbom.verify(Path(destination))
            for path in self.directory.glob("*.nupkg"):
                self.assertEqual(path.read_bytes(), (Path(destination) / path.name).read_bytes())
            self.assertEqual((self.directory / "output").read_text(), "reused=true\n")

    def test_failed_generation_validation_stops_before_copy(self):
        with patch.object(sbom, "stage_inputs", side_effect=lambda p, d: d.mkdir()), \
                patch.object(sbom.subprocess, "run", side_effect=[None, RuntimeError("validation failed")]), \
                tempfile.TemporaryDirectory() as work:
            directory = Path(work)
            for package in sbom.package_set(self.directory):
                (directory / package.name).write_bytes(package.read_bytes())
            with self.assertRaisesRegex(RuntimeError, "validation failed"):
                sbom.generate(directory)
            self.assertEqual(list((directory / "sbom").iterdir()), [])


if __name__ == "__main__":
    unittest.main()
