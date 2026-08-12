namespace DagAlgorithms.Net;

/// <summary>
/// Centralized, named error message text for exceptions thrown by this library.
/// Keeping every message here avoids duplicated string literals scattered across the codebase.
/// </summary>
internal static class GraphErrorMessages
{
    internal const string NodeNotInGraph = "The specified node does not exist in the graph.";

    internal const string CycleDetectedPrefix = "Cycle detected: ";

    internal const string CycleEdgeSeparator = " -> ";

    internal const string MaxDegreeOfParallelismMustBePositive =
        "Maximum degree of parallelism must be greater than or equal to " +
        nameof(GraphScheduler.MinimumMaxDegreeOfParallelism) + ".";
}
