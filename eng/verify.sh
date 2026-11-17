#!/usr/bin/env bash
set -euo pipefail
task_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$task_root"
export DOTNET_PROCESSOR_COUNT="${DOTNET_PROCESSOR_COUNT:-2}"
dotnet build SwiftDispatch.slnx -c Release --disable-build-servers -m:1
dotnet test tests/SwiftDispatch.Tests -c Release --no-build
dotnet pack src/SwiftDispatch.Contracts -c Release --no-build --disable-build-servers -m:1 -o artifacts/packages
dotnet pack src/SwiftDispatch -c Release --no-build --disable-build-servers -m:1 -o artifacts/packages
package_cache="$(mktemp -d "$task_root/artifacts/package-cache.XXXXXX")"
dotnet restore tests/PackageSmoke --packages "$package_cache" --source "$task_root/artifacts/packages" --source https://api.nuget.org/v3/index.json
for framework in net8.0 net10.0 net11.0; do
    dotnet run --project tests/PackageSmoke -c Release -f "$framework" --no-restore --disable-build-servers
    native_output="artifacts/package-native/$framework"
    dotnet publish tests/PackageSmoke -c Release -f "$framework" -r linux-x64 --disable-build-servers -m:1 -p:RestorePackagesPath="$package_cache" -o "$native_output" --source "$task_root/artifacts/packages" --source https://api.nuget.org/v3/index.json
    "./$native_output/PackageSmoke"
done
