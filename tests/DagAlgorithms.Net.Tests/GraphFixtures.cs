using DagAlgorithms.Net;

namespace DagAlgorithms.Net.Tests;

/// <summary>
/// Hand-verified graph fixtures shared across the test suite. Every fixture documents
/// the exact edges it adds and, where relevant, the insertion order that the
/// deterministic algorithms under test rely on.
/// </summary>
internal static class GraphFixtures
{
    /// <summary>
    /// A -> B, A -> C, B -> D, C -> D. Node insertion order: A, B, C, D.
    /// </summary>
    internal static DirectedGraph<string> Diamond()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        graph.AddEdge("B", "D");
        graph.AddEdge("C", "D");
        return graph;
    }

    /// <summary>
    /// A -> B -> C -> D. Node insertion order: A, B, C, D.
    /// </summary>
    internal static DirectedGraph<string> LinearChain()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "D");
        return graph;
    }

    /// <summary>
    /// A disconnected diamond (A, B, C, D) plus a separate chain E -> F. Node insertion
    /// order: A, B, C, D, E, F.
    /// </summary>
    internal static DirectedGraph<string> DisconnectedDiamondAndChain()
    {
        var graph = Diamond();
        graph.AddEdge("E", "F");
        return graph;
    }

    /// <summary>
    /// An empty graph with no nodes and no edges.
    /// </summary>
    internal static DirectedGraph<string> Empty() => new();

    /// <summary>
    /// A single node with no edges.
    /// </summary>
    internal static DirectedGraph<string> SingleIsolatedNode()
    {
        var graph = new DirectedGraph<string>();
        graph.AddNode("Z");
        return graph;
    }

    /// <summary>
    /// Two isolated nodes with no edges between them. Node insertion order: Z, W.
    /// </summary>
    internal static DirectedGraph<string> TwoIsolatedNodes()
    {
        var graph = new DirectedGraph<string>();
        graph.AddNode("Z");
        graph.AddNode("W");
        return graph;
    }

    /// <summary>
    /// A simple three-node cycle: A -> B -> C -> A. Node insertion order: A, B, C.
    /// </summary>
    internal static DirectedGraph<string> SimpleCycle()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "A");
        return graph;
    }

    /// <summary>
    /// A single node with a self-loop: A -> A.
    /// </summary>
    internal static DirectedGraph<string> SelfLoop()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "A");
        return graph;
    }

    /// <summary>
    /// An acyclic chain X -> Y followed by an unrelated cycle D -> E -> D. Node
    /// insertion order: X, Y, D, E.
    /// </summary>
    internal static DirectedGraph<string> AcyclicChainThenCycle()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("X", "Y");
        graph.AddEdge("D", "E");
        graph.AddEdge("E", "D");
        return graph;
    }

    /// <summary>
    /// Two independent cycles, A -> B -> C -> A and D -> E -> D, joined by a one-way
    /// bridge C -> D that does not create a path back from D or E into A, B, or C.
    /// Node insertion order: A, B, C, D, E.
    /// </summary>
    internal static DirectedGraph<string> TwoCyclesJoinedByOneWayBridge()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "A");
        graph.AddEdge("D", "E");
        graph.AddEdge("E", "D");
        graph.AddEdge("C", "D");
        return graph;
    }

    /// <summary>
    /// A rho-shaped graph: a non-cyclic tail A -> B leading into a cycle B -> C -> B.
    /// The offending cycle is exactly [B, C]; A is reachable on the way in but is not
    /// itself part of the cycle. Node insertion order: A, B, C.
    /// </summary>
    internal static DirectedGraph<string> TailIntoCycle()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "B");
        return graph;
    }

    /// <summary>
    /// Two cycles fused into one strongly connected component: A -> B -> C -> A and
    /// C -> D -> E -> C. Every node can reach every other node. Node insertion order:
    /// A, B, C, D, E.
    /// </summary>
    internal static DirectedGraph<string> TwoCyclesFusedBySharedFeedback()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "A");
        graph.AddEdge("C", "D");
        graph.AddEdge("D", "E");
        graph.AddEdge("E", "C");
        return graph;
    }
}
