#!/usr/bin/env -S dotnet --
#:property TargetFramework=net10.0
#:property PublishAot=false
#:include ReleaseTools.cs

using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TrueMediator.ReleaseTools;

// A dependency-free offline harness. HttpClient and process calls are injected;
// these tests cannot contact GitHub/NuGet or push a package.
var tests = new (string Name, Func<Fixture, Task> Run)[]
{
    ("Prerelease and symbol packages", f =>
    {
        File.WriteAllText(Path.Combine(f.Directory, "ignored.snupkg"), "symbols");
        Sbom.Verify(f.Directory);
        return Task.CompletedTask;
    }),
    ("Missing manifest blocks publication", async f =>
    {
        File.Delete(f.Manifest());
        await ExpectFailureAsync(() => f.Client.Stage(f.Directory), ".sbom.spdx.json");
        Tool.Require(f.Requests.Count == 0, "Network request made before local validation");
    }),
    ("Missing analyzer rejected", f =>
    {
        f.Mutate(d => d["files"]!.AsArray().Remove(d["files"]!.AsArray().Single(n => Tool.Text(n!, "fileName").Contains("analyzers/"))));
        ExpectFailure(() => Sbom.Verify(f.Directory), "inventory");
        return Task.CompletedTask;
    }),
    ("Wrong root rejected", f =>
    {
        f.Mutate(d => d["packages"]![0]!["versionInfo"] = "3.0.0");
        ExpectFailure(() => Sbom.Verify(f.Directory), "root");
        return Task.CompletedTask;
    }),
    ("Wrong SPDX format rejected", f =>
    {
        f.Mutate(d => d["spdxVersion"] = "SPDX-3.0");
        ExpectFailure(() => Sbom.Verify(f.Directory), "SPDX-2.2");
        return Task.CompletedTask;
    }),
    ("Wrong hash rejected", f =>
    {
        f.Mutate(d => d["files"]![0]!["checksums"]![0]!["checksumValue"] = new string('0', 64));
        ExpectFailure(() => Sbom.Verify(f.Directory), "SHA256");
        return Task.CompletedTask;
    }),
    ("Missing restore dependency rejected", f =>
    {
        File.WriteAllText(f.Graph(), """{"libraries":{"ThirdParty/1.2.3":{"type":"package"}}}""");
        ExpectFailure(() => Sbom.Verify(f.Directory), "Restore dependencies missing");
        return Task.CompletedTask;
    }),
    ("Mismatched project reference version rejected", f =>
    {
        File.WriteAllText(f.Graph(), """{"libraries":{"TrueMediator.Contracts/2.0.0":{"type":"project"}}}""");
        ExpectFailure(() => Sbom.Verify(f.Directory), "Restore dependencies missing");
        return Task.CompletedTask;
    }),
    ("Failed original validation blocks retry", f =>
    {
        File.WriteAllText(Path.Combine(f.Directory, "validation", $"TrueMediator.{Fixture.Version}.validation.json"),
            """{"Result":"Failure","ValidationErrors":{"Count":1}}""");
        ExpectFailure(() => Sbom.Verify(f.Directory), "validation did not succeed");
        return Task.CompletedTask;
    }),
    ("Partial NuGet publication reuses original packages", async f =>
    {
        var packages = Sbom.Packages(f.Directory);
        foreach (var package in packages.Where((_, index) => index != 1)) f.Publish(package);
        await f.Client.PublishPackages(f.Directory);
        Tool.Require(f.Pushes.Count == 1 && f.Pushes[0].Contains(packages[1].Path), "Incorrect packages pushed");
        Tool.Require(!f.Pushes[0].Contains("--skip-duplicate"), "Duplicate packages skipped without verification");
    }),
    ("Missing release asset blocks all NuGet pushes", async f =>
    {
        f.Assets.Clear();
        await ExpectFailureAsync(() => f.Client.PublishPackages(f.Directory), "assets do not match");
        Tool.Require(f.Pushes.Count == 0, "A package was pushed before asset verification");
        Tool.Require(!f.Requests.Any(r => r.Url.Host == "api.nuget.org"), "NuGet was contacted before asset verification");
    }),
    ("Signed NuGet package must match original", async f =>
    {
        var package = Sbom.Packages(f.Directory)[0];
        f.Publish(package, signature: true);
        Tool.Require(await f.Client.ExistingNuget(package), "Signed package was not recognized");
        f.Publish(package, signature: true, extraFile: true);
        await ExpectFailureAsync(async () => { await f.Client.ExistingNuget(package); }, "different bytes");
    }),
    ("Existing asset is never replaced", async f =>
    {
        f.Assets[Path.GetFileName(f.Manifest())] = Encoding.UTF8.GetBytes("different");
        await ExpectFailureAsync(() => f.Client.Stage(f.Directory), "Refusing to replace");
        Tool.Require(f.Requests.All(r => r.Method == HttpMethod.Get), "An existing asset was mutated");
    }),
    ("Retry without original bundle fails for existing release", async f =>
    {
        await ExpectFailureAsync(() => f.Client.Restore(f.Directory), "manual recovery");
    }),
    ("Draft lookup paginates the authenticated release list", async f =>
    {
        for (var index = 0; index < 100; index++)
            f.ReleaseListPrefix.Add(JsonSerializer.SerializeToNode(new { id = index + 2, tag_name = $"v0.0.{index}", draft = true })!);
        await f.Client.PublishPackages(f.Directory);
        Tool.Require(f.Pushes.Count == 4, "Draft could not be found before package publication");
        Tool.Require(f.Requests.Any(r => r.Url.Query.Contains("page=2", StringComparison.Ordinal)), "Release list was not paginated");
        Tool.Require(f.Requests.Where(r => r.Url.AbsolutePath.EndsWith("/releases", StringComparison.Ordinal))
            .All(r => r.Authorization == "Bearer test-token"), "Draft lookup was not authenticated");
    }),
    ("Existing draft is reused without creating a duplicate", async f =>
    {
        await f.Client.Stage(f.Directory);
        await f.Client.Stage(f.Directory);
        Tool.Require(f.Requests.All(r => r.Method == HttpMethod.Get), "Restaging an existing draft created or changed a release");
    }),
    ("Ambiguous draft releases stop publication", async f =>
    {
        f.ReleaseListPrefix.Add(f.Release!.DeepClone());
        await ExpectFailureAsync(() => f.Client.PublishPackages(f.Directory), "Multiple releases");
        Tool.Require(f.Pushes.Count == 0, "Published packages for ambiguous releases");
    }),
    ("Retry restores byte-identical original bundle", async f =>
    {
        f.Bundle = f.ArchiveBundle();
        using var destination = new TemporaryDirectory();
        await f.Client.Restore(destination.Path);
        Sbom.Verify(destination.Path);
        foreach (var package in Sbom.Packages(f.Directory))
            Tool.Require(File.ReadAllBytes(package.Path).AsSpan().SequenceEqual(
                File.ReadAllBytes(Path.Combine(destination.Path, Path.GetFileName(package.Path)))), "Recovered package bytes differ");
        Tool.Require(File.ReadAllText(f.Output) == "reused=true\n", "Restore did not skip regeneration");
    }),
    ("Recovery uses original outputs and tag with updated workflow code", async f =>
    {
        f.Bundle = f.ArchiveBundle();
        Environment.SetEnvironmentVariable("RELEASE_SOURCE_RUN_ID", "42");
        Environment.SetEnvironmentVariable("RELEASE_TAG", "v" + Fixture.Version);
        Environment.SetEnvironmentVariable("RELEASE_COMMIT", "abc123");
        Environment.SetEnvironmentVariable("GITHUB_RUN_ID", "99");
        Environment.SetEnvironmentVariable("GITHUB_REF_NAME", "master");
        Environment.SetEnvironmentVariable("GITHUB_SHA", "new-workflow-commit");
        using var destination = new TemporaryDirectory();
        await f.Client.Restore(destination.Path);
        await f.Client.Stage(destination.Path);
        await f.Client.PublishPackages(destination.Path);
        Tool.Require(f.Pushes.Count == 4 && File.ReadAllText(f.Output) == "reused=true\n", "Recovery did not reuse original outputs");
        Tool.Require(f.Requests.Any(r => r.Url.AbsolutePath.EndsWith("/actions/runs/42/artifacts", StringComparison.Ordinal)),
            "Recovery used its own run instead of the original one");
    }),
    ("Recovery rejects unrelated original run", async f =>
    {
        Environment.SetEnvironmentVariable("RELEASE_SOURCE_RUN_ID", "42");
        f.SourceRun["head_sha"] = "unrelated-commit";
        await ExpectFailureAsync(() => f.Client.Restore(f.Directory), "does not match the release tag and commit");
        Tool.Require(f.Requests.Count == 1, "Recovery proceeded after provenance check failed");
    }),
    ("Recovery never rebuilds when original artifact is missing", async f =>
    {
        Environment.SetEnvironmentVariable("RELEASE_SOURCE_RUN_ID", "42");
        await ExpectFailureAsync(() => f.Client.Restore(f.Directory), "Original release bundle is missing");
        Tool.Require(!File.Exists(f.Output), "Recovery permitted a rebuild without original outputs");
    }),
    ("Recovery rejects mismatched package version", async f =>
    {
        f.Bundle = f.ArchiveBundle();
        Environment.SetEnvironmentVariable("RELEASE_SOURCE_RUN_ID", "42");
        Environment.SetEnvironmentVariable("RELEASE_TAG", "v3.0.1");
        f.SourceRun["head_branch"] = "v3.0.1";
        using var destination = new TemporaryDirectory();
        await ExpectFailureAsync(() => f.Client.Restore(destination.Path), "versions do not match the release tag");
    }),
    ("Failed tool validation stops before copying manifests", f =>
    {
        using var destination = new TemporaryDirectory();
        foreach (var package in Sbom.Packages(f.Directory)) File.Copy(package.Path, Path.Combine(destination.Path, Path.GetFileName(package.Path)));
        var calls = 0;
        ExpectFailure(() => Sbom.Generate(destination.Path, _ =>
        {
            if (++calls == 2) throw new InvalidOperationException("validation failed");
        }, (_, inputs) => System.IO.Directory.CreateDirectory(inputs)), "validation failed");
        Tool.Require(calls == 2 && !System.IO.Directory.EnumerateFileSystemEntries(Path.Combine(destination.Path, "sbom")).Any(),
            "Generation continued after failed validation");
        return Task.CompletedTask;
    }),
    ("Fresh run creates draft and verifies all uploaded assets", async f =>
    {
        f.Release = null;
        f.Assets.Clear();
        await f.Client.Stage(f.Directory);
        Tool.Require(f.Release!["draft"]!.GetValue<bool>() && f.Assets.Count == 4, "Draft release or assets missing");
        Tool.Require(Tool.Text(f.Release, "body").Split("Download SBOM (SPDX JSON)").Length == 5, "Missing package download links");
        Tool.Require(f.Requests.Count(r => r.Method == HttpMethod.Post) == 5, "Unexpected release mutations");
        Tool.Require(f.Requests.Where(r => r.Url.AbsolutePath.StartsWith("/assets/", StringComparison.Ordinal))
            .All(r => r.Accept == "application/octet-stream"), "Draft assets were not downloaded using the API");
    }),
    ("Publish checks anonymous downloads and supports completed retry", async f =>
    {
        await f.Client.PublishRelease(f.Directory);
        Tool.Require(!f.Release!["draft"]!.GetValue<bool>(), "Release remained draft");
        await f.Client.PublishRelease(f.Directory);
        Tool.Require(f.Requests.Count(r => r.Method == HttpMethod.Patch) == 1, "Published release was modified on retry");
        Tool.Require(f.Requests.Where(r => r.Url.Host == "github.com").All(r => r.Authorization is null), "Public download required authentication");
    }),
    ("ZIP traversal is rejected", f =>
    {
        using var stream = new MemoryStream(Fixture.Archive(new() { ["../escape.dll"] = [1, 2, 3] }));
        using var destination = new TemporaryDirectory();
        ExpectFailure(() => Sbom.Extract(stream, destination.Path), "Unsafe package path");
        return Task.CompletedTask;
    })
};

var failures = 0;
foreach (var (name, run) in tests)
{
    try
    {
        using var fixture = new Fixture();
        await run(fixture);
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception error)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {name}: {error}");
    }
}
Console.WriteLine($"{tests.Length - failures}/{tests.Length} release gate tests passed.");
return failures == 0 ? 0 : 1;

static void ExpectFailure(Action action, string message)
{
    try { action(); }
    catch (Exception error) when (error.Message.Contains(message, StringComparison.Ordinal)) { return; }
    throw new InvalidOperationException($"Expected failure containing: {message}");
}

static async Task ExpectFailureAsync(Func<Task> action, string message)
{
    try { await action(); }
    catch (Exception error) when (error.Message.Contains(message, StringComparison.Ordinal)) { return; }
    throw new InvalidOperationException($"Expected failure containing: {message}");
}

internal sealed class Fixture : IDisposable
{
    public const string Version = "3.0.0-rc.1";
    private readonly TemporaryDirectory work = new();
    private readonly Dictionary<string, string?> environment = [];
    private readonly HttpClient http;
    public string Directory => work.Path;
    public string Output => Path.Combine(Directory, "output");
    public ReleaseClient Client { get; }
    public Dictionary<string, byte[]> Assets { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, byte[]> Published { get; } = new(StringComparer.Ordinal);
    public List<string[]> Pushes { get; } = [];
    public List<RecordedRequest> Requests { get; } = [];
    public byte[]? Bundle { get; set; }
    public List<JsonNode> ReleaseListPrefix { get; } = [];
    public JsonNode SourceRun { get; } = JsonNode.Parse("""
        {"head_sha":"abc123","head_branch":"v3.0.0-rc.1","event":"push",
         "path":".github/workflows/publish.yml","status":"completed"}
        """)!;
    public JsonNode? Release { get; set; } = JsonNode.Parse("""
        {"id":1,"tag_name":"v3.0.0-rc.1","target_commitish":"abc123","draft":true,
         "upload_url":"https://uploads.github.com/repos/Kamyab7/TrueMediator/releases/1/assets{?name,label}"}
        """);

    public Fixture()
    {
        foreach (var (name, value) in new Dictionary<string, string?>
        {
            ["GITHUB_REPOSITORY"] = "Kamyab7/TrueMediator", ["GITHUB_REF_NAME"] = "v" + Version,
            ["GITHUB_SHA"] = "abc123", ["GITHUB_RUN_ID"] = "42", ["GITHUB_OUTPUT"] = Output,
            ["GH_TOKEN"] = "test-token", ["NUGET_API_KEY"] = "test-key",
            ["RELEASE_TAG"] = null, ["RELEASE_COMMIT"] = null, ["RELEASE_SOURCE_RUN_ID"] = null
        })
        {
            environment[name] = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }
        System.IO.Directory.CreateDirectory(Path.Combine(Directory, "sbom"));
        foreach (var name in Sbom.Projects)
        {
            var payload = new Dictionary<string, byte[]>
            {
                [$"lib/net11.0/{name}.dll"] = Encoding.UTF8.GetBytes("library"),
                [$"{name}.nuspec"] = Encoding.UTF8.GetBytes($"""
                    <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata>
                    <id>{name}</id><version>{Version}</version></metadata></package>
                    """)
            };
            if (name == "TrueMediator") payload["analyzers/dotnet/cs/TrueMediator.SourceGenerator.dll"] = Encoding.UTF8.GetBytes("generator");
            File.WriteAllBytes(Path.Combine(Directory, $"{name}.{Version}.nupkg"), Archive(payload));
            var inputs = Path.Combine(Directory, "validation", $"{name}.{Version}.inputs");
            System.IO.Directory.CreateDirectory(inputs);
            File.WriteAllText(Path.Combine(inputs, "project.assets.json"), """{"libraries":{}}""");
            File.WriteAllText(Path.Combine(Directory, "validation", $"{name}.{Version}.validation.json"),
                """{"Result":"Success","ValidationErrors":{"Count":0}}""");
            Tool.WriteJson(Manifest(name), new
            {
                spdxVersion = "SPDX-2.2", SPDXID = "SPDXRef-DOCUMENT",
                packages = new[] { new { SPDXID = "SPDXRef-root", name, versionInfo = Version } },
                relationships = new[] { new { spdxElementId = "SPDXRef-DOCUMENT", relationshipType = "DESCRIBES", relatedSpdxElement = "SPDXRef-root" } },
                files = payload.Select(p => new { fileName = "./" + p.Key,
                    checksums = new[] { new { algorithm = "SHA256", checksumValue = Convert.ToHexStringLower(SHA256.HashData(p.Value)) } } }).ToArray()
            });
            Assets[Path.GetFileName(Manifest(name))] = File.ReadAllBytes(Manifest(name));
        }
        http = new HttpClient(new FakeHandler(Respond));
        Client = new ReleaseClient(http, Pushes.Add);
    }

    public string Manifest(string name = "TrueMediator") => Path.Combine(Directory, "sbom", $"{name}.{Version}.sbom.spdx.json");
    public string Graph() => Path.Combine(Directory, "validation", $"TrueMediator.{Version}.inputs/project.assets.json");
    public byte[] ArchiveBundle() => Archive(System.IO.Directory.GetFiles(Directory, "*", SearchOption.AllDirectories)
        .ToDictionary(p => Tool.Relative(Directory, p), File.ReadAllBytes));
    public void Mutate(Action<JsonNode> change)
    {
        var document = Tool.ReadJson(Manifest());
        change(document);
        File.WriteAllText(Manifest(), document.ToJsonString());
    }

    public static byte[] Archive(Dictionary<string, byte[]> files)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
            foreach (var (name, data) in files)
            {
                using var stream = archive.CreateEntry(name).Open();
                stream.Write(data);
            }
        return output.ToArray();
    }

    public void Publish(Package package, bool signature = false, bool extraFile = false)
    {
        using var archive = ZipFile.OpenRead(package.Path);
        var files = archive.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var stream = e.Open();
            using var output = new MemoryStream();
            stream.CopyTo(output);
            return output.ToArray();
        });
        if (signature) files[".signature.p7s"] = [1, 2, 3];
        if (extraFile) files["extra.dll"] = [4, 5, 6];
        Published[Path.GetFileName(package.Path).ToLowerInvariant()] = Archive(files);
    }

    private async Task<HttpResponseMessage> Respond(HttpRequestMessage message)
    {
        var url = message.RequestUri!;
        var data = message.Content is null ? null : await message.Content.ReadAsByteArrayAsync();
        Requests.Add(new(message.Method, url, message.Headers.Authorization?.ToString(),
            string.Join(",", message.Headers.Accept), data));
        static HttpResponseMessage Bytes(byte[]? bytes) => bytes is null ? new(HttpStatusCode.NotFound)
            : new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };
        static HttpResponseMessage Json(object? value) => value is null ? new(HttpStatusCode.NotFound)
            : Bytes(JsonSerializer.SerializeToUtf8Bytes(value));
        if (url.Host == "api.nuget.org") return Bytes(Published.GetValueOrDefault(Path.GetFileName(url.AbsolutePath)));
        if (url.AbsolutePath.Contains("/actions/runs/", StringComparison.Ordinal) && !url.AbsolutePath.EndsWith("/artifacts", StringComparison.Ordinal))
            return Json(SourceRun);
        if (url.AbsolutePath.Contains("/actions/runs/", StringComparison.Ordinal))
            return Json(new { artifacts = Bundle is null ? Array.Empty<object>() :
                [new { name = "release-bundle", expired = false, archive_download_url = "https://api.github.com/bundle" }] });
        if (url.AbsolutePath == "/bundle") return Bytes(Bundle);
        // GitHub's by-tag endpoint returns published releases, never drafts.
        if (url.AbsolutePath.Contains("/releases/tags/", StringComparison.Ordinal))
            return Json(Release?["draft"]?.GetValue<bool>() == false ? Release : null);
        if (message.Method == HttpMethod.Get && url.AbsolutePath.EndsWith("/releases", StringComparison.Ordinal))
        {
            var page = url.Query.Split('&').FirstOrDefault(p => p.StartsWith("page=", StringComparison.Ordinal));
            var number = page is null ? 1 : int.Parse(page["page=".Length..], System.Globalization.CultureInfo.InvariantCulture);
            var releases = ReleaseListPrefix.Concat(Release is null ? [] : new[] { Release });
            return Json(releases.Skip((number - 1) * 100).Take(100).ToArray());
        }
        if (message.Method == HttpMethod.Post && url.Host == "uploads.github.com")
        {
            Assets[Uri.UnescapeDataString(url.Query["?name=".Length..])] = data!;
            return Json(new { id = 99 });
        }
        if (message.Method == HttpMethod.Post && url.AbsolutePath.EndsWith("/releases", StringComparison.Ordinal))
        {
            Release = JsonNode.Parse(data!)!;
            Release["id"] = 1;
            Release["upload_url"] = "https://uploads.github.com/repos/Kamyab7/TrueMediator/releases/1/assets{?name,label}";
            return Json(Release);
        }
        if (message.Method == HttpMethod.Patch)
        {
            Release!["draft"] = false;
            return Json(Release);
        }
        if (url.AbsolutePath.EndsWith("/assets", StringComparison.Ordinal))
            return Json(Assets.Keys.Select(name => new { name, url = "https://api.github.com/assets/" + name,
                browser_download_url = "https://github.com/Kamyab7/TrueMediator/releases/download/v" + Version + "/" + name }).ToArray());
        if (url.AbsolutePath.StartsWith("/assets/", StringComparison.Ordinal) || url.Host == "github.com")
            return Bytes(Assets.GetValueOrDefault(Path.GetFileName(url.AbsolutePath)));
        throw new InvalidOperationException($"Unexpected HTTP request: {message.Method} {url}");
    }

    public void Dispose()
    {
        http.Dispose();
        foreach (var (name, value) in environment) Environment.SetEnvironmentVariable(name, value);
        work.Dispose();
    }
}

internal sealed record RecordedRequest(HttpMethod Method, Uri Url, string? Authorization, string Accept, byte[]? Data);
internal sealed class FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => respond(request);
}
