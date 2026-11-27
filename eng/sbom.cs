#!/usr/bin/env -S dotnet --
#:property TargetFramework=net10.0
#:property PublishAot=false
#:include ReleaseTools.cs

using TrueMediator.ReleaseTools;

if (args.Length != 2 || args[0] is not ("generate" or "verify"))
{
    Console.Error.WriteLine("Usage: dotnet run --file eng/sbom.cs -- <generate|verify> <package-directory>");
    return 1;
}
try
{
    var directory = Path.GetFullPath(args[1]);
    if (args[0] == "generate") Sbom.Generate(directory);
    else Sbom.Verify(directory);
    Console.WriteLine($"SBOM {args[0]} succeeded: {directory}");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
