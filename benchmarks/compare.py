#!/usr/bin/env python3
"""Turn a BenchmarkDotNet full JSON export into pairwise comparisons."""
import argparse
import json
from itertools import combinations
from pathlib import Path


def comparison(left, right, unit):
    if left == right:
        return f"equal ({left:,.2f} {unit})"
    if min(left, right) == 0:
        return f"{'A' if left < right else 'B'} allocates 0 {unit}; other allocates {max(left, right):,.2f} {unit}"
    winner = 'A' if left < right else 'B'
    low, high = sorted((left, right))
    if unit == 'ns':
        return f"{winner}: {high / low:.2f}x speed, {(1 - low / high) * 100:.1f}% less time"
    return f"{winner}: {(1 - low / high) * 100:.1f}% fewer bytes ({high - low:,.2f} B saved)"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('report', type=Path, help='BenchmarkDotNet *-report-full.json')
    args = parser.parse_args()
    data = json.loads(args.report.read_text())
    groups = {}
    for row in data['Benchmarks']:
        if not row.get('Statistics') or not row.get('Memory'):
            raise SystemExit('Report contains failed or missing measurements; rerun the suite.')
        method = row['Method']
        library, scenario = method.split('_', 1)
        groups.setdefault(scenario, []).append((library, row['Statistics']['Mean'], row['Memory']['BytesAllocatedPerOperation']))
    print('# Pairwise benchmark comparisons\n')
    print('A/B refer to the libraries in each row. Speed is inverse mean elapsed time.\n')
    print('These are point estimates; inspect the original report errors before drawing conclusions.\n')
    print('| Scenario | A | B | Speed | Allocations per operation |')
    print('| --- | --- | --- | --- | --- |')
    for scenario, rows in groups.items():
        if len(rows) != 3:
            raise SystemExit(f'Expected all three libraries for {scenario}; found {len(rows)}.')
        for a, b in combinations(rows, 2):
            print(f'| {scenario} | {a[0]} | {b[0]} | {comparison(a[1], b[1], "ns")} | {comparison(a[2], b[2], "B")} |')


if __name__ == '__main__':
    main()
