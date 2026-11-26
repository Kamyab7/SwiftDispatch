using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SwiftDispatch.ReleaseTools;

internal static class Tool
{
    public static readonly string Root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SourcePath())!, ".."));
    public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static string SourcePath([CallerFilePath] string path = "") => path;
    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    public static string Env(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Missing environment variable: {name}");
    public static JsonNode ReadJson(string path) => JsonNode.Parse(File.ReadAllText(path))!;
    public static IEnumerable<JsonNode> Items(JsonNode? node) => node?.AsArray().Select(n => n!) ?? [];
    public static string Text(JsonNode node, string property) => node[property]!.GetValue<string>();
    public static void WriteJson(string path, object value) =>
        File.WriteAllText(path, JsonSerializer.Serialize(value, JsonOptions) + "\n");
    public static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

    public static void Run(string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = Root, UseShellExecute = false };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Cannot start dotnet");
        process.WaitForExit();
        // Do not print arguments: NuGet push arguments contain the publishing token.
        Require(process.ExitCode == 0, $"dotnet {arguments[0]} failed with exit code {process.ExitCode}");
    }

    public static void CopyTree(string source, string destination, bool excludeBuild = false)
    {
        Require(!Directory.Exists(destination), $"Destination already exists: {destination}");
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        foreach (var directory in Directory.GetDirectories(source))
        {
            var name = Path.GetFileName(directory);
            if (excludeBuild && name is "bin" or "obj") continue;
            CopyTree(directory, Path.Combine(destination, name), excludeBuild);
        }
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("swiftdispatch-sbom-").FullName;
    public void Dispose() => Directory.Delete(Path, recursive: true);
}

internal sealed record Package(string Path, string Name, string Version, XElement Metadata)
{
    public string Stem => $"{Name}.{Version}";
    public string SbomName => $"{Stem}.sbom.spdx.json";
}

internal static class Sbom
{
    public static readonly string[] Projects = ["SwiftDispatch", "SwiftDispatch.Contracts",
        "SwiftDispatch.FluentValidation", "SwiftDispatch.EntityFrameworkCore"];

    public static Package Metadata(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var specs = archive.Entries.Where(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal)
            && !e.FullName.Contains('/')).ToArray();
        Tool.Require(specs.Length == 1, $"Expected one root nuspec: {path}");
        using var stream = specs[0].Open();
        var metadata = XDocument.Load(stream).Root!.Elements().Single(e => e.Name.LocalName == "metadata");
        string Field(string name) => metadata.Elements().Single(e => e.Name.LocalName == name).Value;
        var name = Field("id");
        var version = Field("version");
        Tool.Require(Projects.Contains(name), $"Unknown shipping package: {name}");
        Tool.Require(Regex.IsMatch(version, @"\A[0-9A-Za-z.+-]+\z"), "Invalid package version");
        return new Package(path, name, version, metadata);
    }

    public static Package[] Packages(string directory)
    {
        // .snupkg symbol packages are intentionally separate.
        var packages = Directory.GetFiles(directory, "*.nupkg").Order(StringComparer.Ordinal).Select(Metadata).ToArray();
        Tool.Require(packages.Length == Projects.Length && packages.Select(p => p.Name).ToHashSet().SetEquals(Projects),
            "Expected exactly one final nupkg for each shipping project");
        Tool.Require(packages.Select(p => p.Version).Distinct().Count() == 1, "Shipping package versions must agree");
        return packages;
    }

    public static void Extract(Stream stream, string destination)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        foreach (var entry in archive.Entries)
            Tool.Require(!Path.IsPathRooted(entry.FullName) && !entry.FullName.Contains('\\')
                && !entry.FullName.Split('/').Contains(".."), $"Unsafe package path: {entry.FullName}");
        archive.ExtractToDirectory(destination, overwriteFiles: false);
    }

    public static void Extract(string package, string destination)
    {
        using var stream = File.OpenRead(package);
        Extract(stream, destination);
    }

    public static void StageInputs(string project, string destination, HashSet<string>? visited = null)
    {
        visited ??= [];
        project = Path.GetFullPath(project);
        if (!visited.Add(project)) return;
        Tool.Require(project.StartsWith(Path.Combine(Tool.Root, "src") + Path.DirectorySeparatorChar, StringComparison.Ordinal),
            $"Non-shipping project reference: {project}");
        var source = Path.GetDirectoryName(project)!;
        var assets = Path.Combine(source, "obj/project.assets.json");
        Tool.Require(File.Exists(assets), $"Missing release restore graph: {assets}");
        var target = Path.Combine(destination, Path.GetRelativePath(Tool.Root, source));
        Tool.CopyTree(source, target, excludeBuild: true);
        Directory.CreateDirectory(Path.Combine(target, "obj"));
        File.Copy(assets, Path.Combine(target, "obj/project.assets.json"));
        foreach (var reference in XDocument.Load(project).Descendants().Where(e => e.Name.LocalName == "ProjectReference"))
            StageInputs(Path.Combine(source, reference.Attribute("Include")!.Value), destination, visited);
    }

    public static object CheckManifest(string directory, string manifest, Package package, string inputs)
    {
        var document = Tool.ReadJson(manifest);
        Tool.Require(Tool.Text(document, "spdxVersion") == "SPDX-2.2", "Expected SPDX-2.2");
        var packages = Tool.Items(document["packages"]).ToArray();
        var described = Tool.Items(document["relationships"]).Where(r =>
            Tool.Text(r, "spdxElementId") == Tool.Text(document, "SPDXID") && Tool.Text(r, "relationshipType") == "DESCRIBES")
            .Select(r => Tool.Text(r, "relatedSpdxElement")).ToHashSet();
        var roots = packages.Where(p => described.Contains(Tool.Text(p, "SPDXID"))).ToArray();
        Tool.Require(roots.Length == 1 && Tool.Text(roots[0], "name") == package.Name
            && Tool.Text(roots[0], "versionInfo") == package.Version, $"Wrong SBOM root: {package.Name} {package.Version}");
        var actual = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Where(p => !Tool.Relative(directory, p).Split('/').Contains("_manifest"))
            .ToDictionary(p => Tool.Relative(directory, p), StringComparer.Ordinal);
        var entries = Tool.Items(document["files"]).ToArray();
        string FileName(JsonNode entry)
        {
            var name = Tool.Text(entry, "fileName");
            return name.StartsWith("./", StringComparison.Ordinal) ? name[2..] : name;
        }
        Tool.Require(entries.Select(FileName).Distinct().Count() == entries.Length,
            "Duplicate SBOM file inventory entries");
        var listed = entries.ToDictionary(FileName, StringComparer.Ordinal);
        Tool.Require(listed.Keys.ToHashSet().SetEquals(actual.Keys),
            $"SBOM file inventory differs from {package.Name} (including lib/analyzer DLLs)");
        foreach (var (path, file) in actual)
        {
            using var stream = File.OpenRead(file);
            var hash = Convert.ToHexStringLower(SHA256.HashData(stream));
            Tool.Require(Tool.Items(listed[path]["checksums"]).Any(h => Tool.Text(h, "algorithm") == "SHA256"
                && Tool.Text(h, "checksumValue").Equals(hash, StringComparison.OrdinalIgnoreCase)),
                $"Missing or incorrect SHA256: {path}");
        }
        var detected = packages.Select(p => (Name: Tool.Text(p, "name").ToLowerInvariant(),
            Version: p["versionInfo"]?.GetValue<string>().ToLowerInvariant() ?? "")).ToHashSet();
        var expected = new HashSet<(string Name, string Version)>();
        var targets = new SortedDictionary<string, SortedDictionary<string, string[]>>(StringComparer.Ordinal);
        Tool.Require(Directory.Exists(inputs), $"Original build inputs missing for {package.Name}");
        var graphs = Directory.GetFiles(inputs, "project.assets.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
        Tool.Require(graphs.Length > 0, $"Original restore graphs missing for {package.Name}");
        foreach (var graph in graphs)
        {
            var assets = Tool.ReadJson(graph);
            var frameworks = new SortedDictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var target in assets["targets"]?.AsObject() ?? [])
                frameworks[target.Key] = target.Value!.AsObject().Select(p => p.Key).Order(StringComparer.Ordinal).ToArray();
            targets[Tool.Relative(inputs, graph)] = frameworks;
            foreach (var library in assets["libraries"]!.AsObject())
            {
                var split = library.Key.LastIndexOf('/');
                var name = library.Key[..split];
                var type = Tool.Text(library.Value!, "type");
                if (type == "package" || (type == "project" && Projects.Contains(name)))
                    expected.Add((name.ToLowerInvariant(), library.Key[(split + 1)..].ToLowerInvariant()));
            }
        }
        Tool.Require(expected.IsSubsetOf(detected), $"Restore dependencies missing from {package.Name}: "
            + string.Join(", ", expected.Except(detected).Order()));
        foreach (var dependency in package.Metadata.Descendants().Where(e => e.Name.LocalName == "dependency"))
            Tool.Require(detected.Any(p => p.Name.Equals(dependency.Attribute("id")!.Value, StringComparison.OrdinalIgnoreCase)),
                $"Nuspec dependency missing from {package.Name}: {dependency.Attribute("id")!.Value}");
        return new
        {
            package = package.Name, version = package.Version, files = actual.Count,
            restorePackages = expected.Order().Select(p => new[] { p.Name, p.Version }).ToArray(),
            detectedPackages = detected.Order().Select(p => new[] { p.Name, p.Version }).ToArray(),
            restoreTargets = targets,
            nuspecDependencyGroups = package.Metadata.Descendants().Where(e => e.Name.LocalName == "group").Select(g => new
            {
                framework = g.Attribute("targetFramework")?.Value ?? "all",
                dependencies = g.Elements().Where(e => e.Name.LocalName == "dependency")
                    .Select(d => d.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value)).ToArray()
            }).ToArray()
        };
    }

    public static void Generate(string directory, Action<string[]>? run = null, Action<string, string>? stage = null)
    {
        run ??= Tool.Run;
        stage ??= (project, destination) => StageInputs(project, destination);
        var packages = Packages(directory);
        var output = Path.Combine(directory, "sbom");
        var reports = Path.Combine(directory, "validation");
        Tool.Require(!Directory.Exists(output) && !Directory.Exists(reports), "SBOM outputs already exist; reuse original release outputs");
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(reports);
        foreach (var package in packages)
        {
            using var work = new TemporaryDirectory();
            var payload = Path.Combine(work.Path, "package");
            var inputs = Path.Combine(work.Path, "inputs");
            Extract(package.Path, payload);
            stage(Path.Combine(Tool.Root, "src", package.Name, package.Name + ".csproj"), inputs);
            // The detector omits project references from restore graphs. Include their
            // identities from the actual final nuspecs without rewriting the graphs.
            foreach (var referenced in packages)
            {
                var referencedDirectory = Path.Combine(inputs, "src", referenced.Name);
                if (referenced.Name == package.Name || !Directory.Exists(referencedDirectory)) continue;
                using var archive = ZipFile.OpenRead(referenced.Path);
                var spec = archive.Entries.Single(e => e.FullName.EndsWith(".nuspec", StringComparison.Ordinal) && !e.FullName.Contains('/'));
                spec.ExtractToFile(Path.Combine(referencedDirectory, "obj", spec.FullName));
            }
            foreach (var props in Directory.GetFiles(Tool.Root, "Directory.*.props"))
                File.Copy(props, Path.Combine(inputs, Path.GetFileName(props)));
            run(["tool", "run", "sbom-tool", "--", "generate", "-b", payload, "-bc", inputs,
                "-pn", package.Name, "-pv", package.Version, "-ps", "Kamyab Valipour",
                "-nsb", "https://github.com/Kamyab7/SwiftDispatch/sbom", "-mi", "SPDX:2.2"]);
            run(["tool", "run", "sbom-tool", "--", "validate", "-b", payload, "-mi", "SPDX:2.2",
                "-o", Path.Combine(reports, package.Stem + ".validation.json")]);
            var manifest = Path.Combine(payload, "_manifest/spdx_2.2/manifest.spdx.json");
            Tool.WriteJson(Path.Combine(reports, package.Stem + ".coverage.json"), CheckManifest(payload, manifest, package, inputs));
            Tool.CopyTree(inputs, Path.Combine(reports, package.Stem + ".inputs"));
            File.Copy(manifest, Path.Combine(output, package.SbomName));
        }
        Verify(directory);
    }

    public static void Verify(string directory)
    {
        var expected = new HashSet<string>(StringComparer.Ordinal);
        foreach (var package in Packages(directory))
        {
            expected.Add(package.SbomName);
            var validation = Tool.ReadJson(Path.Combine(directory, "validation", package.Stem + ".validation.json"));
            Tool.Require(validation["Result"]?.GetValue<string>() == "Success" && validation["ValidationErrors"]?["Count"]?.GetValue<int>() == 0,
                $"Microsoft SBOM validation did not succeed for {package.Name}");
            using var work = new TemporaryDirectory();
            Extract(package.Path, work.Path);
            CheckManifest(work.Path, Path.Combine(directory, "sbom", package.SbomName), package,
                Path.Combine(directory, "validation", package.Stem + ".inputs"));
        }
        Tool.Require(Directory.GetFileSystemEntries(Path.Combine(directory, "sbom")).Select(Path.GetFileName).ToHashSet().SetEquals(expected),
            "Expected exactly one versioned SBOM asset per nupkg");
    }
}

internal sealed class ReleaseClient(HttpClient http, Action<string[]>? run = null)
{
    private readonly Action<string[]> runProcess = run ?? Tool.Run;
    private static string Repository => Tool.Env("GITHUB_REPOSITORY");
    private static string TagPath => "releases/tags/" + Uri.EscapeDataString(Tool.Env("GITHUB_REF_NAME"));

    private async Task<byte[]?> Request(string url, byte[]? data = null, string? method = null,
        bool github = true, string accept = "application/vnd.github+json")
    {
        using var message = new HttpRequestMessage(new HttpMethod(method ?? (data is null ? "GET" : "POST")), url);
        message.Headers.UserAgent.ParseAdd("SwiftDispatch-release");
        if (github)
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Tool.Env("GH_TOKEN"));
            message.Headers.Accept.ParseAdd(accept);
            message.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        }
        if (data is not null)
        {
            message.Content = new ByteArrayContent(data);
            message.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }
        using var response = await http.SendAsync(message);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task<JsonNode?> Api(string path, object? data = null, string? method = null)
    {
        var result = await Request($"https://api.github.com/repos/{Repository}/{path}",
            data is null ? null : JsonSerializer.SerializeToUtf8Bytes(data), method);
        return result is null ? null : JsonNode.Parse(result);
    }

    public async Task Restore(string directory)
    {
        var artifacts = (await Api($"actions/runs/{Tool.Env("GITHUB_RUN_ID")}/artifacts?per_page=100"))!;
        var matches = Tool.Items(artifacts["artifacts"]).Where(a => Tool.Text(a, "name") == "release-bundle").ToArray();
        Tool.Require(matches.Length <= 1, "Ambiguous original release bundle");
        if (matches.Length == 1)
        {
            Tool.Require(!matches[0]["expired"]!.GetValue<bool>(), "Original release outputs expired; recover and review them manually");
            var data = await Request(Tool.Text(matches[0], "archive_download_url"));
            Tool.Require(data is not null, "Original release bundle is unavailable");
            using var stream = new MemoryStream(data!);
            Sbom.Extract(stream, directory);
            Sbom.Verify(directory);
        }
        else
            Tool.Require(await Api(TagPath) is null,
                "Release already exists without this run's original bundle; manual recovery required");
        await File.AppendAllTextAsync(Tool.Env("GITHUB_OUTPUT"), $"reused={(matches.Length == 1 ? "true" : "false")}\n");
    }

    private async Task<JsonNode> ReleaseForTag()
    {
        var release = await Api(TagPath) ?? throw new InvalidOperationException("Missing draft release");
        Tool.Require(Tool.Text(release, "target_commitish") == Tool.Env("GITHUB_SHA"), "Release commit differs from this run");
        return release;
    }

    private static string ReleaseId(JsonNode release) => release["id"]!.ToJsonString();
    private async Task<JsonNode[]> Assets(JsonNode release) =>
        Tool.Items(await Api($"releases/{ReleaseId(release)}/assets?per_page=100")).ToArray();

    private async Task CheckAssets(string directory, JsonNode release)
    {
        var expected = Directory.GetFiles(Path.Combine(directory, "sbom"), "*.json").ToDictionary(p => Path.GetFileName(p), StringComparer.Ordinal);
        var actual = (await Assets(release)).Where(a => Tool.Text(a, "name").EndsWith(".sbom.spdx.json", StringComparison.Ordinal))
            .ToDictionary(a => Tool.Text(a, "name"), StringComparer.Ordinal);
        Tool.Require(actual.Keys.ToHashSet().SetEquals(expected.Keys), "Release SBOM assets do not match final packages");
        foreach (var (name, path) in expected)
        {
            var data = await Request(Tool.Text(actual[name], "url"), accept: "application/octet-stream");
            Tool.Require(data is not null && data.AsSpan().SequenceEqual(File.ReadAllBytes(path)), $"Downloaded release asset differs: {name}");
        }
    }

    public async Task<bool> ExistingNuget(Package package)
    {
        var name = package.Name.ToLowerInvariant();
        var version = package.Version.ToLowerInvariant();
        var data = await Request($"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{name}.{version}.nupkg", github: false);
        if (data is null) return false;
        // NuGet adds its repository signature. Every other ZIP entry must match.
        static Dictionary<string, byte[]> Contents(Stream stream)
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            return archive.Entries.Where(e => !e.FullName.EndsWith('/') && e.FullName != ".signature.p7s").ToDictionary(e => e.FullName, e =>
            {
                using var input = e.Open();
                using var output = new MemoryStream();
                input.CopyTo(output);
                return output.ToArray();
            }, StringComparer.Ordinal);
        }
        var original = Contents(File.OpenRead(package.Path));
        var published = Contents(new MemoryStream(data));
        Tool.Require(original.Count == published.Count && original.All(p => published.TryGetValue(p.Key, out var content)
            && p.Value.AsSpan().SequenceEqual(content)),
            $"NuGet already contains different bytes for {package.Name} {package.Version}; manual review required");
        return true;
    }

    public async Task Stage(string directory)
    {
        Sbom.Verify(directory);
        foreach (var package in Sbom.Packages(directory)) await ExistingNuget(package);
        var tag = Tool.Env("GITHUB_REF_NAME");
        var release = await Api(TagPath);
        if (release is null)
        {
            var links = string.Join("\n", Directory.GetFiles(Path.Combine(directory, "sbom"), "*.json").Order(StringComparer.Ordinal).Select(path =>
            {
                var name = Path.GetFileName(path);
                return $"- {name[..^".sbom.spdx.json".Length]}: [Download SBOM (SPDX JSON)](https://github.com/{Repository}/releases/download/{tag}/{name})";
            }));
            release = (await Api("releases", new { tag_name = tag, target_commitish = Tool.Env("GITHUB_SHA"), name = tag,
                draft = true, prerelease = tag.Contains('-'), generate_release_notes = true,
                body = $"## Software Bill of Materials\n\n{links}" }))!;
        }
        Tool.Require(Tool.Text(release, "target_commitish") == Tool.Env("GITHUB_SHA"), "Release commit mismatch");
        var assets = (await Assets(release)).ToDictionary(a => Tool.Text(a, "name"), StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(Path.Combine(directory, "sbom"), "*.json").Order(StringComparer.Ordinal))
        {
            var name = Path.GetFileName(path);
            if (assets.TryGetValue(name, out var asset))
            {
                var data = await Request(Tool.Text(asset, "url"), accept: "application/octet-stream");
                Tool.Require(data is not null && data.AsSpan().SequenceEqual(File.ReadAllBytes(path)), $"Refusing to replace existing SBOM: {name}");
            }
            else
            {
                Tool.Require(release["draft"]!.GetValue<bool>(), "Cannot add an asset to a published release");
                var url = Tool.Text(release, "upload_url").Split('{')[0] + "?name=" + Uri.EscapeDataString(name);
                await Request(url, File.ReadAllBytes(path));
            }
        }
        await CheckAssets(directory, release);
    }

    public async Task PublishPackages(string directory)
    {
        Sbom.Verify(directory);
        await CheckAssets(directory, await ReleaseForTag());
        var packages = Sbom.Packages(directory);
        var existing = new HashSet<string>();
        foreach (var package in packages)
            if (await ExistingNuget(package)) existing.Add(package.Path);
        foreach (var package in packages)
            if (!existing.Contains(package.Path))
                runProcess(["nuget", "push", package.Path, "--api-key", Tool.Env("NUGET_API_KEY"),
                    "--source", "https://api.nuget.org/v3/index.json"]);
    }

    public async Task PublishRelease(string directory)
    {
        Sbom.Verify(directory);
        var release = await ReleaseForTag();
        await CheckAssets(directory, release);
        if (release["draft"]!.GetValue<bool>()) await Api($"releases/{ReleaseId(release)}", new { draft = false }, "PATCH");
        foreach (var asset in await Assets(release))
        {
            var name = Tool.Text(asset, "name");
            if (!name.EndsWith(".sbom.spdx.json", StringComparison.Ordinal)) continue;
            var data = await Request(Tool.Text(asset, "browser_download_url"), github: false);
            Tool.Require(data is not null && data.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(directory, "sbom", name))),
                $"Public SBOM download differs: {name}");
        }
    }
}
