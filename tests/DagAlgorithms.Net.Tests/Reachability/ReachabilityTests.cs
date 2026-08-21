namespace DagAlgorithms.Net.Tests.Reachability;

public class ReachabilityTests
{
    private const int DeepNodeCount = 10_000;

    [Fact]
    public void GetDescendants_over_a_linear_chain_returns_everything_downstream()
    {
        var graph = GraphFixtures.LinearChain();

        Assert.Equal(new[] { "B", "C", "D" }, graph.GetDescendants("A"));
    }

    [Fact]
    public void GetAncestors_over_a_linear_chain_returns_everything_upstream()
    {
        var graph = GraphFixtures.LinearChain();

        Assert.Equal(new[] { "A", "B", "C" }, Sorted(graph.GetAncestors("D")));
    }

    [Fact]
    public void GetDescendants_of_a_sink_node_is_empty()
    {
        var graph = GraphFixtures.LinearChain();

        Assert.Empty(graph.GetDescendants("D"));
    }

    [Fact]
    public void GetAncestors_of_a_source_node_is_empty()
    {
        var graph = GraphFixtures.LinearChain();

        Assert.Empty(graph.GetAncestors("A"));
    }

    [Fact]
    public void GetDescendants_over_a_diamond_deduplicates_the_shared_sink()
    {
        var graph = GraphFixtures.Diamond();

        Assert.Equal(new[] { "B", "C", "D" }, Sorted(graph.GetDescendants("A")));
    }

    [Fact]
    public void GetAncestors_over_a_diamond_deduplicates_the_shared_source()
    {
        var graph = GraphFixtures.Diamond();

        Assert.Equal(new[] { "A", "B", "C" }, Sorted(graph.GetAncestors("D")));
    }

    [Fact]
    public void GetDescendants_does_not_cross_into_a_disconnected_component()
    {
        var graph = GraphFixtures.DisconnectedDiamondAndChain();

        Assert.Equal(new[] { "B", "C", "D" }, Sorted(graph.GetDescendants("A")));
        Assert.DoesNotContain("E", graph.GetDescendants("A"));
        Assert.DoesNotContain("F", graph.GetDescendants("A"));
    }

    [Fact]
    public void GetDescendants_of_an_isolated_node_is_empty()
    {
        var graph = GraphFixtures.SingleIsolatedNode();

        Assert.Empty(graph.GetDescendants("Z"));
    }

    [Fact]
    public void GetAncestors_of_an_isolated_node_is_empty()
    {
        var graph = GraphFixtures.SingleIsolatedNode();

        Assert.Empty(graph.GetAncestors("Z"));
    }

    [Fact]
    public void GetDescendants_throws_for_a_node_not_in_the_graph()
    {
        var graph = GraphFixtures.LinearChain();

        Assert.Throws<ArgumentException>(() => graph.GetDescendants("missing"));
    }

    [Fact]
    public void GetAncestors_throws_for_a_node_not_in_the_graph()
    {
        var graph = GraphFixtures.LinearChain();

        Assert.Throws<ArgumentException>(() => graph.GetAncestors("missing"));
    }

    [Fact]
    public void GetDescendants_and_GetAncestors_throw_for_a_null_node()
    {
        var graph = GraphFixtures.LinearChain();
        string? nullNode = null;

        Assert.Throws<ArgumentNullException>(() => graph.GetDescendants(nullNode!));
        Assert.Throws<ArgumentNullException>(() => graph.GetAncestors(nullNode!));
    }

    [Fact]
    public void GetDescendants_returns_the_same_order_on_repeated_calls()
    {
        var graph = GraphFixtures.Diamond();

        var first = graph.GetDescendants("A");
        var second = graph.GetDescendants("A");

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetAncestors_returns_the_same_order_on_repeated_calls()
    {
        var graph = GraphFixtures.Diamond();

        var first = graph.GetAncestors("D");
        var second = graph.GetAncestors("D");

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetDescendants_handles_a_deep_dependency_chain_without_a_stack_overflow()
    {
        var graph = BuildLinearChain(DeepNodeCount);
        var names = NodeNames(DeepNodeCount);

        var descendants = graph.GetDescendants(names[0]);

        Assert.Equal(DeepNodeCount - 1, descendants.Count);
        Assert.DoesNotContain(names[0], descendants);
        Assert.Contains(names[^1], descendants);
    }

    [Fact]
    public void GetAncestors_handles_a_deep_dependency_chain_without_a_stack_overflow()
    {
        var graph = BuildLinearChain(DeepNodeCount);
        var names = NodeNames(DeepNodeCount);

        var ancestors = graph.GetAncestors(names[^1]);

        Assert.Equal(DeepNodeCount - 1, ancestors.Count);
        Assert.DoesNotContain(names[^1], ancestors);
        Assert.Contains(names[0], ancestors);
    }

    private static string[] Sorted(IEnumerable<string> nodes) =>
        nodes.OrderBy(n => n, StringComparer.Ordinal).ToArray();

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
}
