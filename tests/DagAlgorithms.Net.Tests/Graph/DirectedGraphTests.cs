namespace DagAlgorithms.Net.Tests.Graph;

public class DirectedGraphTests
{
    [Fact]
    public void AddNode_returns_true_for_a_new_node()
    {
        var graph = new DirectedGraph<string>();
        Assert.True(graph.AddNode("A"));
        Assert.Equal(1, graph.NodeCount);
    }

    [Fact]
    public void AddNode_returns_false_for_a_duplicate_node()
    {
        var graph = new DirectedGraph<string>();
        graph.AddNode("A");
        Assert.False(graph.AddNode("A"));
        Assert.Equal(1, graph.NodeCount);
    }

    [Fact]
    public void AddEdge_adds_missing_endpoint_nodes_automatically()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");

        Assert.Equal(2, graph.NodeCount);
        Assert.True(graph.ContainsNode("A"));
        Assert.True(graph.ContainsNode("B"));
    }

    [Fact]
    public void AddEdge_returns_true_for_a_new_edge_and_false_for_a_duplicate()
    {
        var graph = new DirectedGraph<string>();
        Assert.True(graph.AddEdge("A", "B"));
        Assert.False(graph.AddEdge("A", "B"));
        Assert.Equal(1, graph.EdgeCount);
    }

    [Fact]
    public void EdgeCount_reflects_only_distinct_edges()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        graph.AddEdge("A", "B");

        Assert.Equal(2, graph.EdgeCount);
    }

    [Fact]
    public void Nodes_are_returned_in_first_seen_insertion_order()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddNode("A");
        graph.AddEdge("C", "A");

        Assert.Equal(new[] { "A", "B", "C" }, graph.Nodes);
    }

    [Fact]
    public void GetSuccessors_reflects_edges_in_insertion_order()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "C");
        graph.AddEdge("A", "B");

        Assert.Equal(new[] { "C", "B" }, graph.GetSuccessors("A"));
    }

    [Fact]
    public void GetPredecessors_reflects_edges_in_insertion_order()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("C", "A");
        graph.AddEdge("B", "A");

        Assert.Equal(new[] { "C", "B" }, graph.GetPredecessors("A"));
    }

    [Fact]
    public void GetSuccessors_for_a_leaf_node_is_empty()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");

        Assert.Empty(graph.GetSuccessors("B"));
    }

    [Fact]
    public void GetPredecessors_for_a_root_node_is_empty()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");

        Assert.Empty(graph.GetPredecessors("A"));
    }

    [Fact]
    public void GetSuccessors_throws_for_a_node_not_in_the_graph()
    {
        var graph = new DirectedGraph<string>();
        graph.AddNode("A");

        Assert.Throws<ArgumentException>(() => graph.GetSuccessors("missing"));
    }

    [Fact]
    public void GetPredecessors_throws_for_a_node_not_in_the_graph()
    {
        var graph = new DirectedGraph<string>();
        graph.AddNode("A");

        Assert.Throws<ArgumentException>(() => graph.GetPredecessors("missing"));
    }

    [Fact]
    public void ContainsNode_is_true_for_added_nodes_and_false_otherwise()
    {
        var graph = new DirectedGraph<string>();
        graph.AddNode("A");

        Assert.True(graph.ContainsNode("A"));
        Assert.False(graph.ContainsNode("B"));
    }

    [Fact]
    public void Default_comparer_is_used_when_none_is_supplied()
    {
        var graph = new DirectedGraph<string>();
        Assert.Same(EqualityComparer<string>.Default, graph.Comparer);
    }

    [Fact]
    public void Custom_comparer_treats_equivalent_keys_as_the_same_node()
    {
        var graph = new DirectedGraph<string>(StringComparer.OrdinalIgnoreCase);
        graph.AddNode("a");

        Assert.False(graph.AddNode("A"));
        Assert.Equal(1, graph.NodeCount);
        Assert.True(graph.ContainsNode("A"));
    }

    [Fact]
    public void AddNode_throws_for_a_null_node()
    {
        var graph = new DirectedGraph<string>();
        string? nullNode = null;

        Assert.Throws<ArgumentNullException>(() => graph.AddNode(nullNode!));
    }

    [Fact]
    public void AddEdge_throws_for_a_null_endpoint()
    {
        var graph = new DirectedGraph<string>();
        string? nullNode = null;

        Assert.Throws<ArgumentNullException>(() => graph.AddEdge(nullNode!, "A"));
        Assert.Throws<ArgumentNullException>(() => graph.AddEdge("A", nullNode!));
    }

    [Fact]
    public void Self_loop_edge_is_recorded_as_both_a_successor_and_a_predecessor()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "A");

        Assert.Equal(new[] { "A" }, graph.GetSuccessors("A"));
        Assert.Equal(new[] { "A" }, graph.GetPredecessors("A"));
    }
}
