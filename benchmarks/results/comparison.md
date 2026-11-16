# Pairwise benchmark comparisons

A/B refer to the libraries in each row. Speed is inverse mean elapsed time.

These are point estimates; inspect the original report errors before drawing conclusions.

| Scenario | A | B | Speed | Allocations per operation |
| --- | --- | --- | --- | --- |
| Send | MediatR | Mediator | B: 1.63x speed, 38.6% less time | B: 56.0% fewer bytes (112.00 B saved) |
| Send | MediatR | SwiftDispatch | B: 1.89x speed, 47.2% less time | B: 32.0% fewer bytes (64.00 B saved) |
| Send | Mediator | SwiftDispatch | B: 1.16x speed, 14.0% less time | A: 35.3% fewer bytes (48.00 B saved) |
| Publish | MediatR | Mediator | B: 5.87x speed, 83.0% less time | B: 92.7% fewer bytes (712.00 B saved) |
| Publish | MediatR | SwiftDispatch | B: 3.94x speed, 74.6% less time | B: 76.0% fewer bytes (584.00 B saved) |
| Publish | Mediator | SwiftDispatch | A: 1.49x speed, 32.9% less time | A: 69.6% fewer bytes (128.00 B saved) |
| AsyncSend | MediatR | Mediator | A: 1.12x speed, 11.0% less time | B: 14.3% fewer bytes (32.00 B saved) |
| AsyncSend | MediatR | SwiftDispatch | B: 1.03x speed, 3.3% less time | B: 28.6% fewer bytes (64.00 B saved) |
| AsyncSend | Mediator | SwiftDispatch | B: 1.16x speed, 13.9% less time | B: 16.7% fewer bytes (32.00 B saved) |
