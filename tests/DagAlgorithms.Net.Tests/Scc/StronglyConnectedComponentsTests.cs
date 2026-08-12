namespace DagAlgorithms.Net.Tests.Scc;

public class StronglyConnectedComponentsTests
{
    public static IEnumerable<object[]> Fixtures()
    {
        yield return new object[]
        {
            "acyclic diamond: every node is its own component",
            GraphFixtures.Diamond(),
            new[] { new[] { "A" }, new[] { "B" }, new[] { "C" }, new[] { "D" } },
        };
        yield return new object[]
        {
            "disconnected diamond and chain: every node is its own component",
            GraphFixtures.DisconnectedDiamondAndChain(),
            new[] { new[] { "A" }, new[] { "B" }, new[] { "C" }, new[] { "D" }, new[] { "E" }, new[] { "F" } },
        };
        yield return new object[]
        {
            "single three-node cycle merges into one component",
            GraphFixtures.SimpleCycle(),
            new[] { new[] { "A", "B", "C" } },
        };
        yield return new object[]
        {
            "self loop is its own component",
            GraphFixtures.SelfLoop(),
            new[] { new[] { "A" } },
        };
        yield return new object[]
        {
            "two cycles joined by a one-way bridge stay separate components",
            GraphFixtures.TwoCyclesJoinedByOneWayBridge(),
            new[] { new[] { "A", "B", "C" }, new[] { "D", "E" } },
        };
        yield return new object[]
        {
            "two cycles fused by shared feedback merge into one component",
            GraphFixtures.TwoCyclesFusedBySharedFeedback(),
            new[] { new[] { "A", "B", "C", "D", "E" } },
        };
        yield return new object[]
        {
            "isolated nodes are each their own component",
            GraphFixtures.TwoIsolatedNodes(),
            new[] { new[] { "Z" }, new[] { "W" } },
        };
        yield return new object[]
        {
            "empty graph has no components",
            GraphFixtures.Empty(),
            Array.Empty<string[]>(),
        };
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void StronglyConnectedComponents_matches_the_hand_verified_partition(
        string fixtureName, DirectedGraph<string> graph, string[][] expectedComponents)
    {
        var actual = graph.StronglyConnectedComponents();

        var normalizedActual = Normalize(actual, graph);
        var normalizedExpected = expectedComponents
            .Select(component => component.OrderBy(InsertionIndex(graph)).ToArray())
            .OrderBy(component => InsertionIndex(graph)(component[0]))
            .ToArray();

        Assert.True(
            normalizedExpected.Length == normalizedActual.Length,
            $"Fixture '{fixtureName}' expected {normalizedExpected.Length} components but found {normalizedActual.Length}.");
        for (var i = 0; i < normalizedExpected.Length; i++)
        {
            Assert.True(
                normalizedExpected[i].SequenceEqual(normalizedActual[i]),
                $"Component {i} of fixture '{fixtureName}' expected [{string.Join(",", normalizedExpected[i])}] but found [{string.Join(",", normalizedActual[i])}].");
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Every_node_appears_in_exactly_one_component(
        string fixtureName, DirectedGraph<string> graph, string[][] expectedComponents)
    {
        _ = expectedComponents;
        var components = graph.StronglyConnectedComponents();
        var allMembers = components.SelectMany(c => c).ToList();

        Assert.Equal(graph.NodeCount, allMembers.Count);
        Assert.Equal(graph.Nodes.OrderBy(n => n), allMembers.Distinct().OrderBy(n => n));
        _ = fixtureName;
    }

    private static Func<string, int> InsertionIndex(DirectedGraph<string> graph)
    {
        var index = graph.Nodes
            .Select((node, position) => (node, position))
            .ToDictionary(pair => pair.node, pair => pair.position);
        return node => index[node];
    }

    private static string[][] Normalize(IReadOnlyList<IReadOnlyList<string>> components, DirectedGraph<string> graph)
    {
        var byIndex = InsertionIndex(graph);
        return components
            .Select(component => component.OrderBy(byIndex).ToArray())
            .OrderBy(component => component.Length == 0 ? int.MaxValue : byIndex(component[0]))
            .ToArray();
    }
}
