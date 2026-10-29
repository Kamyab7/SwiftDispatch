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
dotnet run --project tests/PackageSmoke -c Release --no-restore --disable-build-servers
dotnet publish tests/PackageSmoke -c Release -r linux-x64 --disable-build-servers -m:1 -p:RestorePackagesPath="$package_cache" -o artifacts/package-native --source "$task_root/artifacts/packages" --source https://api.nuget.org/v3/index.json
./artifacts/package-native/PackageSmoke
