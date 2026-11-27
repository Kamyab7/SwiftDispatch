using BenchmarkDotNet.Running;
using TrueMediator.Benchmarks;

if (args is ["--verify"])
{
    var benchmarks = new DispatchBenchmarks();
    benchmarks.Setup();
    try
    {
        if (await benchmarks.MediatR_Send() != 42 || await benchmarks.Mediator_Send() != 42 ||
            await benchmarks.TrueMediator_Send() != 42 || await benchmarks.MediatR_AsyncSend() != 42 ||
            await benchmarks.Mediator_AsyncSend() != 42 || await benchmarks.TrueMediator_AsyncSend() != 42)
            throw new InvalidOperationException("Request returned an incorrect result.");
        await benchmarks.MediatR_Publish();
        await benchmarks.Mediator_Publish();
        await benchmarks.TrueMediator_Publish();
        if (benchmarks.NotificationCount != 3)
            throw new InvalidOperationException("Notification handler did not execute exactly once per publish.");
        Console.WriteLine("All three libraries passed request and notification verification.");
    }
    finally { benchmarks.Cleanup(); }
    return;
}

BenchmarkSwitcher.FromAssembly(typeof(DispatchBenchmarks).Assembly).Run(args);
