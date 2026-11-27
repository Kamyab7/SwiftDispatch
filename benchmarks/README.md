# Mediator benchmarks

Compare this checkout of TrueMediator with MediatR 15.0.0 and
[martinothamar/Mediator](https://github.com/martinothamar/Mediator) 3.0.2,
using BenchmarkDotNet 0.15.8 on .NET 10. Versions are pinned in
`Directory.Packages.props`.

Run from the repository root with the .NET 10 SDK:

```sh
# Check every request result and notification delivery without timing.
dotnet run -c Release --project benchmarks/TrueMediator.Benchmarks -- --verify

# Full measurement (recommended for published comparisons).
dotnet run -c Release --project benchmarks/TrueMediator.Benchmarks -- --filter '*'

# Faster exploratory run; fewer samples, wider uncertainty.
dotnet run -c Release --project benchmarks/TrueMediator.Benchmarks -- --filter '*' --job short --artifacts benchmarks/BenchmarkDotNet.Artifacts

# Pairwise speed and allocation comparisons for all three libraries.
python3 benchmarks/compare.py benchmarks/BenchmarkDotNet.Artifacts/results/TrueMediator.Benchmarks.DispatchBenchmarks-report-full.json
```

The default full run writes reports under `BenchmarkDotNet.Artifacts/results`
in the current directory. `--artifacts` above puts them under `benchmarks`.
Markdown, CSV, HTML and full JSON exports include machine/runtime information.
Generated artifacts are ignored by Git; selected results can be copied to `results/`.

See the checked-in [exploratory short run](results/short-run.md) and
[pairwise comparisons](results/comparison.md) for an example from this machine.

## What is measured

| Scenario | Identical work in each library |
| --- | --- |
| CompletedRequest | Send a reused reference request; return integer 42 immediately |
| YieldingRequest | Send a reused reference request; await `Task.Yield()` and return 42 |
| Notification | Publish a reused reference notification to one handler that increments a counter |

All libraries are invoked through their `IMediator` interface, resolved once
from the same DI scope. Handlers are transient in all three libraries. Mediator
is explicitly configured as transient instead of its default singleton lifetime;
TrueMediator's mediator is scoped. Scope construction, registration, source
generation, request creation and mediator resolution are outside timed operations.
There are no pipeline behaviors or logging sinks. Notification execution is sequential.
Task APIs are used for MediatR and TrueMediator; Mediator uses its native
ValueTask API. This intentionally includes the allocation difference between those APIs.
The completed Task result is 42, outside the runtime's small-integer Task cache.

These are warmed dispatch microbenchmarks, not startup, throughput under load,
process memory, retained heap, database latency or an overall library ranking.
The yielding scenario also measures thread-pool scheduling and is particularly
sensitive to machine load. Run on an idle machine without a debugger and keep
the runtime, hardware, versions and settings with any published results.

## Reading the results

- **Mean / Error / StdDev**: elapsed time per operation and measurement uncertainty; lower mean is faster.
- **Ratio**: mean time divided by MediatR's mean within the same scenario. `0.50` means half the time (2x speed); `1.20` means 20% more time.
- **Allocated**: managed bytes allocated per operation, including transient handlers; lower uses fewer allocations. It does not measure total process RAM.
- **Alloc Ratio**: allocation relative to MediatR. `0.75` means 25% fewer bytes.
- **Gen0 / Gen1 / Gen2**: garbage collections per 1,000 operations.

`compare.py` prints every pair within each scenario. Speed factor is
`slower mean / faster mean`; time reduction is `100 * (1 - faster / slower)`.
Allocation reduction uses the same percentage formula. Zero allocations are
reported directly rather than dividing by zero. Close results with overlapping
uncertainty should be treated as inconclusive; a short job is exploratory.
