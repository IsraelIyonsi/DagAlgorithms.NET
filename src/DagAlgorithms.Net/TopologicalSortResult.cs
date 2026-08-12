namespace DagAlgorithms.Net;

/// <summary>
/// The outcome of <see cref="DirectedGraph{TNode}.TopologicalSort"/>: either a stable
/// dependency-respecting order over every node, or the exact cycle that made a
/// topological order impossible.
/// </summary>
/// <typeparam name="TNode">The type used to identify nodes in the graph.</typeparam>
public sealed class TopologicalSortResult<TNode>
{
    private TopologicalSortResult(bool isAcyclic, IReadOnlyList<TNode>? order, IReadOnlyList<TNode>? cycle)
    {
        IsAcyclic = isAcyclic;
        Order = order;
        Cycle = cycle;
    }

    /// <summary>
    /// Gets a value indicating whether the graph was acyclic. When <c>true</c>,
    /// <see cref="Order"/> is populated and <see cref="Cycle"/> is <c>null</c>. When
    /// <c>false</c>, the reverse is true.
    /// </summary>
    public bool IsAcyclic { get; }

    /// <summary>
    /// Gets a topological order over every node in the graph: for every edge added
    /// with <c>AddEdge(from, to)</c>, <c>from</c> appears before <c>to</c>. Ties between
    /// nodes with no ordering constraint between them are broken by insertion order.
    /// <c>null</c> when <see cref="IsAcyclic"/> is <c>false</c>.
    /// </summary>
    public IReadOnlyList<TNode>? Order { get; }

    /// <summary>
    /// Gets the node sequence of one cycle found in the graph, in traversal order. The
    /// cycle closes from the last node back to the first; the returned list does not
    /// repeat the first node at the end. <c>null</c> when <see cref="IsAcyclic"/> is
    /// <c>true</c>.
    /// </summary>
    public IReadOnlyList<TNode>? Cycle { get; }

    internal static TopologicalSortResult<TNode> Acyclic(IReadOnlyList<TNode> order) => new(true, order, null);

    internal static TopologicalSortResult<TNode> Cyclic(IReadOnlyList<TNode> cycle) => new(false, null, cycle);
}
