#!/usr/bin/env -S dotnet --
#:property TargetFramework=net10.0
#:property PublishAot=false
#:include ReleaseTools.cs

using SwiftDispatch.ReleaseTools;

if (args.Length != 2 || args[0] is not ("restore" or "stage" or "packages" or "publish"))
{
    Console.Error.WriteLine("Usage: dotnet run --file eng/release.cs -- <restore|stage|packages|publish> <package-directory>");
    return 1;
}
try
{
    using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
    var client = new ReleaseClient(http);
    var directory = Path.GetFullPath(args[1]);
    await (args[0] switch
    {
        "restore" => client.Restore(directory),
        "stage" => client.Stage(directory),
        "packages" => client.PublishPackages(directory),
        _ => client.PublishRelease(directory)
    });
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
