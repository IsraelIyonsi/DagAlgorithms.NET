namespace DagAlgorithms.Net;

/// <summary>
/// Thrown when an operation requires an acyclic graph but the graph contains a cycle.
/// Carries the exact offending node sequence, not just the fact that a cycle exists.
/// </summary>
/// <typeparam name="TNode">The type used to identify nodes in the graph.</typeparam>
public sealed class GraphCycleException<TNode> : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="GraphCycleException{TNode}"/> for the
    /// given cycle.
    /// </summary>
    /// <param name="cycle">
    /// The node sequence forming the cycle, in traversal order, not repeating the first
    /// node at the end.
    /// </param>
    public GraphCycleException(IReadOnlyList<TNode> cycle)
        : base(GraphErrorMessages.CycleDetectedPrefix + string.Join(GraphErrorMessages.CycleEdgeSeparator, cycle))
    {
        ArgumentNullException.ThrowIfNull(cycle);
        Cycle = cycle;
    }

    /// <summary>
    /// Gets the node sequence forming the cycle, in traversal order. The cycle closes
    /// from the last node back to the first.
    /// </summary>
    public IReadOnlyList<TNode> Cycle { get; }
}
