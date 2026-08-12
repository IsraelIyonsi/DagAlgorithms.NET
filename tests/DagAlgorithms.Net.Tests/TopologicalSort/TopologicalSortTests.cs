namespace DagAlgorithms.Net.Tests.TopologicalSort;

public class TopologicalSortTests
{
    public static IEnumerable<object[]> AcyclicFixtures()
    {
        yield return new object[]
        {
            "diamond",
            GraphFixtures.Diamond(),
            new[] { "A", "B", "C", "D" },
        };
        yield return new object[]
        {
            "linear chain",
            GraphFixtures.LinearChain(),
            new[] { "A", "B", "C", "D" },
        };
        yield return new object[]
        {
            "disconnected diamond and chain",
            GraphFixtures.DisconnectedDiamondAndChain(),
            new[] { "A", "B", "C", "D", "E", "F" },
        };
        yield return new object[]
        {
            "empty graph",
            GraphFixtures.Empty(),
            Array.Empty<string>(),
        };
        yield return new object[]
        {
            "single isolated node",
            GraphFixtures.SingleIsolatedNode(),
            new[] { "Z" },
        };
        yield return new object[]
        {
            "two isolated nodes",
            GraphFixtures.TwoIsolatedNodes(),
            new[] { "Z", "W" },
        };
    }

    [Theory]
    [MemberData(nameof(AcyclicFixtures))]
    public void TopologicalSort_returns_the_exact_deterministic_order_for_acyclic_graphs(
        string fixtureName, DirectedGraph<string> graph, string[] expectedOrder)
    {
        var result = graph.TopologicalSort();

        Assert.True(result.IsAcyclic, $"Fixture '{fixtureName}' was expected to be acyclic.");
        Assert.Null(result.Cycle);
        Assert.Equal(expectedOrder, result.Order);
    }

    [Theory]
    [MemberData(nameof(AcyclicFixtures))]
    public void Every_edge_precedes_its_target_in_the_returned_order(
        string fixtureName, DirectedGraph<string> graph, string[] expectedOrder)
    {
        _ = fixtureName;
        _ = expectedOrder;
        var order = graph.TopologicalSort().Order!;
        var positionOf = order
            .Select((node, position) => (node, position))
            .ToDictionary(pair => pair.node, pair => pair.position);

        foreach (var node in graph.Nodes)
        {
            foreach (var successor in graph.GetSuccessors(node))
            {
                Assert.True(
                    positionOf[node] < positionOf[successor],
                    $"Expected '{node}' before '{successor}' in the topological order.");
            }
        }
    }

    public static IEnumerable<object[]> CyclicFixtures()
    {
        yield return new object[] { GraphFixtures.SimpleCycle(), new[] { "A", "B", "C" } };
        yield return new object[] { GraphFixtures.SelfLoop(), new[] { "A" } };
        yield return new object[] { GraphFixtures.AcyclicChainThenCycle(), new[] { "D", "E" } };
        yield return new object[] { GraphFixtures.TwoCyclesJoinedByOneWayBridge(), new[] { "A", "B", "C" } };
        yield return new object[] { GraphFixtures.TailIntoCycle(), new[] { "B", "C" } };
    }

    [Theory]
    [MemberData(nameof(CyclicFixtures))]
    public void TopologicalSort_reports_the_exact_cycle_for_cyclic_graphs(
        DirectedGraph<string> graph, string[] expectedCycle)
    {
        var result = graph.TopologicalSort();

        Assert.False(result.IsAcyclic);
        Assert.Null(result.Order);
        Assert.Equal(expectedCycle, result.Cycle);
    }
}
