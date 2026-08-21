# DagAlgorithms.NET

Directed acyclic graph algorithms for .NET: topological sort, cycle detection that
returns the actual offending path, strongly connected components, and an async
scheduler that runs your work in dependency order with bounded parallelism. Zero
external dependencies.

Most systems eventually need a dependency graph: build steps, deployment stages,
task pipelines, package installs, spreadsheet cell recalculation, feature flag
prerequisites. The moment two of those depend on each other by mistake, you want to
know exactly which nodes are involved, not just "there is a cycle somewhere." QuikGraph
was the closest thing .NET had to a general graph library and it has been dormant on
NuGet since 2022, ships a large general-purpose surface for a narrow need, and pulls in
its own dependency chain. DagAlgorithms.NET is the small, focused, dependency-free
alternative: the four operations you actually reach for when you have a DAG, done
correctly, with the failure paths you can trust in production.

## Install

```
dotnet add package DagAlgorithms.Net
```

## Quickstart: build order from a dependency graph

```csharp
using DagAlgorithms.Net;

var graph = new DirectedGraph<string>();
graph.AddEdge("compile", "test");
graph.AddEdge("compile", "lint");
graph.AddEdge("test", "package");
graph.AddEdge("lint", "package");

var result = graph.TopologicalSort();
if (result.IsAcyclic)
{
    Console.WriteLine(string.Join(" -> ", result.Order!));
    // compile -> test -> lint -> package
}
```

Nodes can be any type with a sensible equality comparer: strings, structs, your own
`record`. Ties between nodes with no ordering constraint between them are broken by
insertion order, so the same graph always produces the same order.

## Reporting the exact cycle, not just "there is one"

A boolean "has a cycle" answer is nearly useless once your graph has more than a
handful of nodes. `TopologicalSort` and `TryDetectCycle` both return the real node
sequence that closes the loop.

```csharp
using DagAlgorithms.Net;

var graph = new DirectedGraph<string>();
graph.AddEdge("OrderService", "PaymentService");
graph.AddEdge("PaymentService", "LedgerService");
graph.AddEdge("LedgerService", "OrderService");

if (graph.TryDetectCycle(out var cycle))
{
    Console.WriteLine($"Circular dependency: {string.Join(" -> ", cycle)} -> {cycle[0]}");
    // Circular dependency: OrderService -> PaymentService -> LedgerService -> OrderService
}
```

## Running work in dependency order

`GraphScheduler.ExecuteAsync` runs an async action per node, starting a node only once
every one of its dependencies has completed, with no more than
`maxDegreeOfParallelism` nodes in flight at a time. If any action throws, the first
failure is captured, every action that has not yet started is cancelled through the
`CancellationToken` your action receives, and the original exception is rethrown with
its stack trace intact once everything has settled.

```csharp
using DagAlgorithms.Net;

var graph = new DirectedGraph<string>();
graph.AddEdge("restore", "build");
graph.AddEdge("build", "unit-tests");
graph.AddEdge("build", "integration-tests");
graph.AddEdge("unit-tests", "publish");
graph.AddEdge("integration-tests", "publish");

await GraphScheduler.ExecuteAsync(
    graph,
    async (stage, cancellationToken) =>
    {
        Console.WriteLine($"Running {stage}");
        await RunPipelineStageAsync(stage, cancellationToken);
    },
    maxDegreeOfParallelism: 4);
```

`unit-tests` and `integration-tests` run concurrently once `build` finishes; `publish`
waits for both.

## Impact analysis: transitive dependencies

`GetDescendants` returns everything reachable by following edges forward;
`GetAncestors` returns everything that reaches a node by following edges backward. This
is the query behind impact analysis: when one build step, package, or service changes,
which others transitively depend on it and must be rebuilt, retested, or redeployed?

```csharp
using DagAlgorithms.Net;

var graph = new DirectedGraph<string>();
graph.AddEdge("core", "orders");
graph.AddEdge("core", "billing");
graph.AddEdge("orders", "checkout");
graph.AddEdge("billing", "checkout");

// "core" just changed. What is downstream of it and needs a rebuild?
foreach (var affected in graph.GetDescendants("core"))
{
    Console.WriteLine(affected);
    // orders, billing, checkout
}

// What must "checkout" be built on top of?
foreach (var dependency in graph.GetAncestors("checkout"))
{
    Console.WriteLine(dependency);
    // orders, billing, core
}
```

Both walk the graph iteratively (an explicit queue plus a visited set, never recursion),
return their results in breadth-first discovery order, and terminate even if the graph
contains a cycle. A node is excluded from its own descendants and ancestors unless a
cycle genuinely leads back to it.

## Strongly connected components

`StronglyConnectedComponents` partitions the graph using Tarjan's algorithm, useful for
finding every mutually-dependent cluster at once rather than one cycle at a time.

```csharp
var components = graph.StronglyConnectedComponents();
foreach (var component in components.Where(c => c.Count > 1))
{
    Console.WriteLine($"Mutually dependent: {string.Join(", ", component)}");
}
```

## API

| Type | Purpose |
|---|---|
| `DirectedGraph<TNode>` | Build the graph: `AddNode`, `AddEdge`, `GetSuccessors`, `GetPredecessors` |
| `DirectedGraph<TNode>.GetDescendants(node)` | Transitive closure over successors: everything downstream, for impact analysis |
| `DirectedGraph<TNode>.GetAncestors(node)` | Transitive closure over predecessors: everything upstream a node depends on |
| `DirectedGraph<TNode>.TopologicalSort()` | Kahn's algorithm; a stable order or the exact cycle |
| `DirectedGraph<TNode>.TryDetectCycle(out cycle)` | DFS cycle-path extraction on its own |
| `DirectedGraph<TNode>.StronglyConnectedComponents()` | Tarjan's algorithm, iterative, no recursion depth limit |
| `GraphScheduler.ExecuteAsync(...)` | Dependency-respecting async execution with bounded parallelism |
| `GraphCycleException<TNode>` | Thrown by the scheduler when the graph is cyclic; carries the cycle |

## Zero dependencies, AOT-friendly

DagAlgorithms.NET has no runtime NuGet dependencies: everything is built on the base
class library. There is no reflection, no dynamic code generation, and no unbound
recursion (both the cycle-path search and Tarjan's algorithm use an explicit,
heap-allocated work stack instead of the call stack, so traversal depth is not limited
by thread stack size). That makes the package trim- and Native AOT-friendly out of the
box.

## Notes and limitations

- Edges are directed and deduplicated: adding the same `(from, to)` pair twice is a
  no-op the second time, and `EdgeCount` only counts distinct edges.
- Self-loops (`AddEdge(node, node)`) are allowed at the graph level and surface as a
  one-node cycle from `TopologicalSort`, `TryDetectCycle`, and
  `StronglyConnectedComponents`, exactly like any other cycle.
- `GraphScheduler.ExecuteAsync` cancels cooperatively. An action that ignores the
  `CancellationToken` it is given still runs to completion after a sibling failure; its
  result is discarded once the first failure is rethrown.

## Roadmap

- `netstandard2.0` target for .NET Framework and older runtimes
- Optional weighted-edge variant for shortest-path-style queries over a DAG
- Incremental topological order maintenance for graphs that mutate after the first sort

## License

MIT. See [LICENSE](LICENSE).
