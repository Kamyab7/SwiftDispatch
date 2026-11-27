# Exploratory short run — 2026-11-16

Versions measured: TrueMediator working checkout (1.0.0), MediatR 15.0.0,
Mediator 3.0.2. See [pairwise comparisons](comparison.md) and [raw JSON](short-run.json).

These historical measurements predate the 2.1.0 release and have not been rerun for
that version.

Three measurement iterations only, on a shared development machine. Repository
build/tests briefly overlapped this run. Treat these results as a working example,
not publication-quality evidence. In particular, yielding-request timing errors
overlap substantially; no reliable asynchronous speed winner is established.
Rerun the full job on an idle machine before making performance claims.

```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04 LTS (Resolute Raccoon)
13th Gen Intel Core i7-13650HX 0.80GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.114
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1
WarmupCount=3

```
| Method                  | Categories       | Mean      | Error      | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------------- |----------:|-----------:|----------:|------:|-------:|----------:|------------:|
| MediatR_Send            | CompletedRequest |  47.57 ns |  39.548 ns |  2.168 ns |  1.00 | 0.0159 |     200 B |        1.00 |
| Mediator_Send           | CompletedRequest |  29.20 ns |   3.711 ns |  0.203 ns |  0.61 | 0.0070 |      88 B |        0.44 |
| TrueMediator_Send      | CompletedRequest |  25.11 ns |   3.895 ns |  0.213 ns |  0.53 | 0.0108 |     136 B |        0.68 |
|                         |                  |           |            |           |       |        |           |             |
| MediatR_Publish         | Notification     | 215.53 ns |  33.141 ns |  1.817 ns |  1.00 | 0.0610 |     768 B |        1.00 |
| Mediator_Publish        | Notification     |  36.72 ns |   4.754 ns |  0.261 ns |  0.17 | 0.0044 |      56 B |        0.07 |
| TrueMediator_Publish   | Notification     |  54.70 ns |  16.452 ns |  0.902 ns |  0.25 | 0.0147 |     184 B |        0.24 |
|                         |                  |           |            |           |       |        |           |             |
| MediatR_AsyncSend       | YieldingRequest  | 888.31 ns | 101.498 ns |  5.563 ns |  1.00 | 0.0172 |     224 B |        1.00 |
| Mediator_AsyncSend      | YieldingRequest  | 998.18 ns | 742.088 ns | 40.676 ns |  1.12 | 0.0153 |     192 B |        0.86 |
| TrueMediator_AsyncSend | YieldingRequest  | 859.18 ns | 502.071 ns | 27.520 ns |  0.97 | 0.0124 |     160 B |        0.71 |
