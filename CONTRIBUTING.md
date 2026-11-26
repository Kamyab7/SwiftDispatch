# Contributing to SwiftDispatch

Contributions are welcome: bug reports, fixes, tests, documentation, and feature proposals all help improve SwiftDispatch.

Everyone participating in this project is expected to follow our [Code of Conduct](CODE_OF_CONDUCT.md).

## Before you start

- Search existing issues and pull requests to avoid duplicate work.
- For a substantial feature, public API change, or breaking change, open an issue first to discuss the use case and approach.
- Keep each pull request focused on one problem. Small documentation and bug fixes can go straight to a pull request.
- Be respectful, explain your reasoning, and keep review discussions focused on the work.

## Report a bug or propose a feature

For suspected security vulnerabilities, follow the private reporting process in [SECURITY.md](SECURITY.md) instead of opening a public issue.

For bugs, include the SwiftDispatch version, .NET SDK and runtime versions (`dotnet --info`), operating system, steps to reproduce, expected behavior, and actual behavior. A minimal runnable example and relevant compiler diagnostics or stack traces are especially useful. Mention whether the issue occurs with Native AOT or trimming.

For features, describe the problem, an example of the desired usage, and any alternatives you considered. Remove secrets and private data from examples and logs.

## Set up your environment

Fork the repository if you do not have write access, clone your fork, and create a descriptive branch for your change.

Install the .NET 11 SDK to build the repository and the .NET 8 and .NET 10 runtimes to run the tests targeting those versions. The test suite targets .NET 8, .NET 10, and .NET 11.

From the repository root:

```sh
dotnet restore SwiftDispatch.slnx
dotnet build SwiftDispatch.slnx -c Release
dotnet test tests/SwiftDispatch.Tests -c Release --no-build
dotnet test tests/SwiftDispatch.FluentValidation.Tests -c Release --no-build
dotnet test tests/SwiftDispatch.EntityFrameworkCore.Tests -c Release --no-build
```

## Find the right place to contribute

| Path | Purpose |
| --- | --- |
| `src/SwiftDispatch.Contracts/` | Public request, notification, streaming, and mediator contracts |
| `src/SwiftDispatch/` | Runtime dispatch, dependency injection, and execution strategies |
| `src/SwiftDispatch.EntityFrameworkCore/` | Optional attribute-selected EF Core transactions |
| `src/SwiftDispatch.FluentValidation/` | Optional asynchronous request validation integration |
| `src/SwiftDispatch.SourceGenerator/` | Registration discovery, diagnostics, and generated dispatch code |
| `tests/SwiftDispatch.Tests/` | xUnit runtime and source generator tests |
| `tests/SwiftDispatch.EntityFrameworkCore.Tests/` | Relational transactions and attribute diagnostics |
| `tests/EfPackageSmoke/` | Packed EF integration consumption under JIT execution |
| `tests/SwiftDispatch.FluentValidation.Tests/` | Validation behavior and generated pipeline integration tests |
| `tests/NetStandardSmoke/` | .NET Standard consumer compatibility coverage |
| `tests/PackageSmoke/` | Packaged-library and Native AOT smoke checks |
| `samples/` | Example applications |
| `website/src/docs/` | Website documentation in Markdown |
| `eng/verify.sh` | End-to-end verification used by CI |

## Make your change

- Follow [`.editorconfig`](.editorconfig) and the conventions in nearby code. C# uses four-space indentation and file-scoped namespaces.
- Keep nullable reference types enabled and resolve build warnings; the repository treats warnings as errors.
- Preserve support for `netstandard2.0`, `net8.0`, `net10.0`, and `net11.0` where applicable. Check the affected project's target frameworks before using newer APIs.
- Preserve Native AOT and trimming compatibility. Changes to discovery or dispatch should account for generated code and avoid introducing runtime reflection requirements.
- Add a regression test for bug fixes and tests for new behavior. Use the runtime or generator tests as appropriate; cover diagnostics and generated-code compilation when changing the source generator.
- Consider cancellation, dependency injection lifetimes, behavior ordering, and notification concurrency when changing related behavior.
- Update the README, website documentation, or samples when usage or public behavior changes.
- Keep unrelated formatting, dependency updates, and generated build output out of your pull request. Package versions are managed centrally in `Directory.Packages.props`.

## Validate your change

During development, run the build and test commands above. For a focused test run, use a filter:

```sh
dotnet test tests/SwiftDispatch.Tests -c Release --filter FullyQualifiedName~NotificationTests
```

Before submitting library, generator, or packaging changes, run the same verification as CI in a Linux x64 environment:

```sh
bash eng/verify.sh
```

The script builds the solution, runs tests, creates NuGet packages, runs a package consumer, and publishes and executes a Native AOT binary for each of .NET 8, .NET 10, and .NET 11. It also packs the EF integration and runs its SQLite consumer under JIT execution for all three frameworks; EF Core is not part of the Native AOT claim. It requires Bash, the .NET SDK/runtime setup above, and native compilation prerequisites. On Ubuntu, CI installs these with:

```sh
sudo apt-get update
sudo apt-get install -y clang zlib1g-dev
```

The script publishes specifically for `linux-x64`; use a matching environment for the complete check. If you cannot run it locally, state which checks you ran and let the pull request's CI verify the Linux Native AOT path.

Verification also restores the pinned Microsoft SBOM tool and generates and validates
an SPDX 2.2 SBOM for each packaged library. Packages and reports are saved in a fresh
`artifacts/verify-packages.*` directory on every run. See the
[release and SBOM guide](eng/RELEASING.md) for scan scope, publication gates, and retry
recovery. Run the offline release gate tests with
`dotnet run --file eng/test-release.cs`.

For website changes, use Node.js `^22.18.0 || >=24.12.0` and npm, then run:

```sh
cd website
npm ci
npm run build
```

Use `npm run dev` to inspect affected pages locally. See the [website README](website/README.md) for documentation structure and linking conventions. Documentation-only changes do not need unrelated library checks.

## Submit a pull request

Use a clear title describing the result. Existing commits commonly use prefixes such as `fix:`, `feat:`, and `docs:`; follow that style where useful.

In the description, include:

- The problem and what the change does, with a linked issue if one exists.
- Tests or verification commands you ran and their results; mention any checks you could not run.
- Compatibility implications, especially public API changes, supported frameworks, trimming, and Native AOT.
- Screenshots for visible website changes, when useful.

Check the diff for accidental files and unrelated changes, ensure CI passes, and respond to review feedback. Use a draft pull request if the work is still in progress.

## License

By submitting a contribution, you agree that it may be distributed under the repository's [MIT license](LICENSE).
