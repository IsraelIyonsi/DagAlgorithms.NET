namespace DagAlgorithms.Net.Tests.StackSafety;

public class DeepGraphTests
{
    private const int DeepNodeCount = 100_000;

    [Fact]
    public void TopologicalSort_handles_a_deep_linear_chain_without_a_stack_overflow()
    {
        var graph = BuildLinearChain(DeepNodeCount);

        var result = graph.TopologicalSort();

        Assert.True(result.IsAcyclic);
        Assert.Equal(NodeNames(DeepNodeCount), result.Order);
    }

    [Fact]
    public void TryDetectCycle_handles_a_deep_cycle_without_a_stack_overflow()
    {
        var graph = BuildCycle(DeepNodeCount);

        var found = graph.TryDetectCycle(out var cycle);

        Assert.True(found);
        Assert.Equal(NodeNames(DeepNodeCount), cycle);
    }

    [Fact]
    public void StronglyConnectedComponents_handles_a_deep_cycle_without_a_stack_overflow()
    {
        var graph = BuildCycle(DeepNodeCount);

        var components = graph.StronglyConnectedComponents();

        Assert.Single(components);
        Assert.Equal(DeepNodeCount, components[0].Count);
        Assert.Equal(
            NodeNames(DeepNodeCount).OrderBy(n => n, StringComparer.Ordinal),
            components[0].OrderBy(n => n, StringComparer.Ordinal));
    }

    private static string[] NodeNames(int count) =>
        Enumerable.Range(0, count).Select(i => $"N{i}").ToArray();

    private static DirectedGraph<string> BuildLinearChain(int nodeCount)
    {
        var graph = new DirectedGraph<string>();
        var names = NodeNames(nodeCount);
        for (var i = 0; i < names.Length - 1; i++)
        {
            graph.AddEdge(names[i], names[i + 1]);
        }

        return graph;
    }

    private static DirectedGraph<string> BuildCycle(int nodeCount)
    {
        var graph = BuildLinearChain(nodeCount);
        var names = NodeNames(nodeCount);
        graph.AddEdge(names[^1], names[0]);
        return graph;
    }
}
