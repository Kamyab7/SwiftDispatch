# NuGet releases and SBOMs

Push a version tag on `master` (for example, `v2.2.0-rc.1`) to run
`.github/workflows/publish.yml`. The existing NuGet trusted publishing login uses
`NUGET_USER`; its repository/environment policy must continue to allow this workflow.
The publish job alone has GitHub `contents: write` for draft release creation and
asset upload, `actions: read` for retry recovery, and `id-token: write` for NuGet.
No signing steps are added or removed; SBOM generation uses the final pack outputs.

The repository tool manifest pins Microsoft.Sbom.DotNetTool 4.1.13, invoked as
`dotnet tool run sbom-tool`. It requires .NET 8, installed by both workflows.
`eng/sbom.py` extracts each final `.nupkg` outside the source tree, reads its identity
from the embedded `.nuspec`, generates SPDX 2.2 JSON, and runs Microsoft validation.
Symbol `.snupkg` files are not included in the package inventory or published by
these steps.

The scan scope is the shipping project plus its recursive project references.
Only their sources and original `obj/project.assets.json` restore graphs are
staged; tests, samples, benchmarks, unrelated shipping projects, and tool packages
are excluded. SwiftDispatch's scope includes its bundled source generator and its
Roslyn build dependencies. Referenced shipping projects' nuspec files are also
copied from the final packages: Microsoft's detector otherwise omits project
references from the restore graphs. The original graphs are never rewritten.
All supported target frameworks are combined into one
manifest per package. These manifests inventory release build inputs as well as
runtime dependencies; they are not an application's deployment inventory.
Bundled generator file presence and SHA256 are checked independently of component
detection. No third-party DLL is currently bundled by these package projects;
review component identities separately if that changes.

Publication requires exactly four packages with a common version, one matching
SBOM per package, the correct SPDX root ID/version, exact file inventory and SHA256
checksums (including library and analyzer DLLs), all NuGet package identities in
the scoped restore graphs, and all `.nuspec` dependency IDs. The CI artifact
contains Microsoft's validation reports, dependency coverage reports, and the
original scoped inputs so detected direct, transitive, and build-only dependencies
can be reviewed. `eng/verify.sh` exercises real generation/validation in normal CI;
`python3 -m unittest discover -s eng -p 'test_*.py' -v` exercises failure gates and
partial publication without making network mutations.

Before any publication, Actions saves the original packages, SBOMs, and evidence in
the `release-bundle` artifact for 90 days. The workflow creates a draft GitHub
Release with per-package **Download SBOM (SPDX JSON)** links, uploads only the four
versioned SBOM JSON files, and downloads and compares every asset before pushing
packages. It publishes the release after successful NuGet pushes, then verifies
unauthenticated public downloads. All assets are attached before publishing, so
the sequence also supports immutable releases. Keep released SBOM assets for as
long as the corresponding NuGet versions are available.

## Retrying a failed release

Use **Re-run failed jobs** on the original Actions run. It restores the original
`release-bundle` and skips build, pack, and generation. It never replaces an
existing release asset. If NuGet already has a version, the workflow downloads it
and compares every ZIP entry with the original package, allowing only NuGet's
added `.signature.p7s` repository signature. A mismatch fails before further pushes;
there is no unconditional `--skip-duplicate`. A race during a push fails safely and
can be retried. Existing identical assets and already published identical packages
are reused. A completed release can also be retried without modifying its assets.

If the original bundle expired, or an existing release belongs to another run,
automatic recovery stops. Recover the original Actions outputs, or the published
packages and original build/restore inputs, into a separate directory. Review
their provenance and dependency coverage and run `python3 eng/sbom.py verify DIR`
before an explicit manual recovery. Do not rebuild and silently replace an SBOM.
Historical versions predating this workflow are not automatically backfilled.

## First release rehearsal

CI validates generated prerelease package identities without publishing. Before
the first production release, use a release candidate tag to also check the live
GitHub/NuGet permissions and direct JSON downloads. Review the saved coverage
reports against the nuspec groups and per-framework restore targets. Simulate a
failure after a subset of packages is pushed and rerun that same Actions run to
confirm original outputs are reused. These live service checks require an actual
release; offline tests do not establish service permissions or repository settings.
