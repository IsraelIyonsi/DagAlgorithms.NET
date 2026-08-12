namespace DagAlgorithms.Net.Tests.Cycles;

public class CycleDetectionTests
{
    public static IEnumerable<object[]> CyclicFixtures()
    {
        yield return new object[] { "simple three-node cycle", GraphFixtures.SimpleCycle(), new[] { "A", "B", "C" } };
        yield return new object[] { "self loop", GraphFixtures.SelfLoop(), new[] { "A" } };
        yield return new object[] { "cycle reachable only from a later component", GraphFixtures.AcyclicChainThenCycle(), new[] { "D", "E" } };
        yield return new object[] { "first of two cycles joined by a one-way bridge", GraphFixtures.TwoCyclesJoinedByOneWayBridge(), new[] { "A", "B", "C" } };
        yield return new object[] { "rho-shaped: acyclic tail leading into a cycle", GraphFixtures.TailIntoCycle(), new[] { "B", "C" } };
    }

    [Theory]
    [MemberData(nameof(CyclicFixtures))]
    public void TryDetectCycle_returns_the_exact_offending_path(string fixtureName, DirectedGraph<string> graph, string[] expectedCycle)
    {
        var found = graph.TryDetectCycle(out var cycle);

        Assert.True(found, $"Fixture '{fixtureName}' was expected to contain a cycle.");
        Assert.Equal(expectedCycle, cycle);
    }

    [Theory]
    [MemberData(nameof(CyclicFixtures))]
    public void Every_reported_cycle_step_is_a_real_edge_and_the_path_closes_into_a_loop(
        string fixtureName, DirectedGraph<string> graph, string[] expectedCycle)
    {
        _ = fixtureName;
        _ = expectedCycle;
        graph.TryDetectCycle(out var cycle);

        for (var i = 0; i < cycle.Count; i++)
        {
            var from = cycle[i];
            var to = cycle[(i + 1) % cycle.Count];
            Assert.Contains(to, graph.GetSuccessors(from));
        }
    }

    public static IEnumerable<object[]> AcyclicFixtures()
    {
        yield return new object[] { GraphFixtures.Diamond() };
        yield return new object[] { GraphFixtures.LinearChain() };
        yield return new object[] { GraphFixtures.DisconnectedDiamondAndChain() };
        yield return new object[] { GraphFixtures.Empty() };
        yield return new object[] { GraphFixtures.TwoIsolatedNodes() };
    }

    [Theory]
    [MemberData(nameof(AcyclicFixtures))]
    public void TryDetectCycle_returns_false_and_an_empty_path_for_acyclic_graphs(DirectedGraph<string> graph)
    {
        var found = graph.TryDetectCycle(out var cycle);

        Assert.False(found);
        Assert.Empty(cycle);
    }
}
