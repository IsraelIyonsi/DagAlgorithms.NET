# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-08-12

### Added

- `DirectedGraph<TNode>`: a generic directed graph over caller-supplied node keys, with
  a configurable `IEqualityComparer<TNode>`, `AddNode`, `AddEdge`, `ContainsNode`,
  `GetSuccessors`, `GetPredecessors`, `Nodes`, `NodeCount`, and `EdgeCount`. Nodes and
  edges are tracked in insertion order for deterministic downstream algorithms.
- `DirectedGraph<TNode>.TopologicalSort()`: Kahn's algorithm with deterministic,
  insertion-order tie-breaking. Returns a `TopologicalSortResult<TNode>` holding either
  the full order or the exact cycle that prevented one.
- `DirectedGraph<TNode>.TryDetectCycle(out cycle)`: standalone cycle-path extraction
  using depth-first search with an explicit, non-recursive traversal stack, returning
  the real node sequence of the cycle rather than a boolean.
- `DirectedGraph<TNode>.StronglyConnectedComponents()`: Tarjan's algorithm implemented
  iteratively with an explicit work stack, so traversal depth is not bounded by the
  runtime call stack.
- `GraphScheduler.ExecuteAsync(...)`: runs a per-node asynchronous action respecting
  every dependency edge, with caller-bounded parallelism via `maxDegreeOfParallelism`.
  On the first failure, the exception is captured, every not-yet-started action is
  cancelled through its `CancellationToken`, and the original exception is rethrown
  with its stack trace preserved once every action has settled.
- `GraphCycleException<TNode>`: thrown by `GraphScheduler.ExecuteAsync` when the graph
  contains a cycle, carrying the exact cycle path.
- Table-driven fixtures covering diamonds, linear chains, disconnected components,
  self-loops, single and multiple cycles (both isolated and fused by shared feedback),
  and bounded-parallelism and first-error-cancels-remaining scheduler behavior.
- Zero runtime dependencies; built entirely on the base class library.
- SourceLink (GitHub), deterministic CI builds, and `.snupkg` symbol packages.
