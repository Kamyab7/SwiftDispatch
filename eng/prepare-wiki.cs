#!/usr/bin/env -S dotnet --
#:property TargetFramework=net10.0
#:property PublishAot=false

using System.Text.RegularExpressions;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: dotnet run --file eng/prepare-wiki.cs -- <docs-directory> <wiki-directory>");
    return 1;
}

try
{
    var source = Path.GetFullPath(args[0]);
    var target = Path.GetFullPath(args[1]);
    var pages = Directory.Exists(source)
        ? Directory.GetFiles(source, "*.md").Order(StringComparer.Ordinal).ToArray()
        : [];
    if (pages.Length == 0 || !File.Exists(Path.Combine(source, "getting-started.md")))
        throw new InvalidOperationException("Expected documentation pages including getting-started.md");

    var names = pages.ToDictionary(page => Path.GetFileName(page), page =>
        Path.GetFileNameWithoutExtension(page) == "getting-started" ? "Home" : Path.GetFileNameWithoutExtension(page),
        StringComparer.Ordinal);
    var rendered = new Dictionary<string, string>(StringComparer.Ordinal);
    var titles = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var page in pages)
    {
        // Match Python's original universal-newline reading for consistent wiki output.
        var text = File.ReadAllText(page).Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        var title = text.Split('\n').FirstOrDefault(line => line.StartsWith("# ", StringComparison.Ordinal));
        if (title is null)
            throw new InvalidOperationException($"Expected a level-one heading in {page}");
        titles[page] = title[2..].Trim();

        // Convert local Markdown page links; preserve fragments and external links.
        var rewritten = Regex.Replace(text, @"\]\(([^)\s]+)\)", match =>
        {
            var destination = match.Groups[1].Value;
            var separator = destination.IndexOf('#');
            var path = separator < 0 ? destination : destination[..separator];
            var fragment = separator < 0 ? "" : destination[separator..];
            if (path.StartsWith("./", StringComparison.Ordinal)) path = path[2..];
            return "](" + (names.TryGetValue(path, out var name) ? name + fragment : destination) + ")";
        });
        rendered[names[Path.GetFileName(page)] + ".md"] = rewritten;
    }

    var sidebar = new List<string> { "<!-- Generated from website/src/docs; edit the source files. -->", "" };
    foreach (var page in pages.OrderBy(p => Path.GetFileName(p) != "getting-started.md").ThenBy(p => p, StringComparer.Ordinal))
        sidebar.Add($"- [{titles[page]}]({names[Path.GetFileName(page)]})");
    rendered["_Sidebar.md"] = string.Join('\n', sidebar) + "\n";

    // Render and validate every page before changing the generated checkout.
    // Remove obsolete pages while retaining Git history and non-Markdown files.
    Directory.CreateDirectory(target);
    foreach (var page in Directory.GetFiles(target, "*.md"))
        if (!rendered.ContainsKey(Path.GetFileName(page))) File.Delete(page);
    foreach (var (name, text) in rendered) File.WriteAllText(Path.Combine(target, name), text);
    Console.WriteLine($"Prepared {pages.Length} wiki pages and sidebar in {target}");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
