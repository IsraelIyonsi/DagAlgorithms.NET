namespace DagAlgorithms.Net;

/// <summary>
/// A generic directed graph over caller-supplied node keys. Supports topological
/// sorting, cycle-path detection, and strongly connected component discovery.
/// </summary>
/// <remarks>
/// Nodes and edges are tracked in insertion order, which every algorithm on this type
/// uses to break ties deterministically: given the same sequence of
/// <see cref="AddNode"/> and <see cref="AddEdge"/> calls, every result is reproducible
/// across runs and machines.
/// </remarks>
/// <typeparam name="TNode">The type used to identify nodes in the graph.</typeparam>
public sealed class DirectedGraph<TNode>
    where TNode : notnull
{
    private enum VisitState : byte
    {
        Unvisited = 0,
        InProgress = 1,
        Done = 2,
    }

    private readonly OrderedSet<TNode> _nodes;
    private readonly Dictionary<TNode, OrderedSet<TNode>> _successorsByNode;
    private readonly Dictionary<TNode, OrderedSet<TNode>> _predecessorsByNode;
    private int _edgeCount;

    /// <summary>
    /// Initializes a new, empty <see cref="DirectedGraph{TNode}"/>.
    /// </summary>
    /// <param name="comparer">
    /// The equality comparer used to identify nodes. Defaults to
    /// <see cref="EqualityComparer{TNode}.Default"/> when omitted.
    /// </param>
    public DirectedGraph(IEqualityComparer<TNode>? comparer = null)
    {
        Comparer = comparer ?? EqualityComparer<TNode>.Default;
        _nodes = new OrderedSet<TNode>(Comparer);
        _successorsByNode = new Dictionary<TNode, OrderedSet<TNode>>(Comparer);
        _predecessorsByNode = new Dictionary<TNode, OrderedSet<TNode>>(Comparer);
    }

    /// <summary>
    /// Gets the equality comparer used to identify nodes in this graph.
    /// </summary>
    public IEqualityComparer<TNode> Comparer { get; }

    /// <summary>
    /// Gets every node in the graph, in the order it was first added, whether directly
    /// through <see cref="AddNode"/> or implicitly through <see cref="AddEdge"/>.
    /// </summary>
    public IReadOnlyList<TNode> Nodes => _nodes;

    /// <summary>
    /// Gets the number of nodes in the graph.
    /// </summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// Gets the number of distinct directed edges in the graph.
    /// </summary>
    public int EdgeCount => _edgeCount;

    /// <summary>
    /// Adds a node to the graph if it is not already present.
    /// </summary>
    /// <param name="node">The node to add.</param>
    /// <returns><c>true</c> if the node was newly added; <c>false</c> if it already existed.</returns>
    public bool AddNode(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodes.Add(node))
        {
            return false;
        }

        _successorsByNode.Add(node, new OrderedSet<TNode>(Comparer));
        _predecessorsByNode.Add(node, new OrderedSet<TNode>(Comparer));
        return true;
    }

    /// <summary>
    /// Adds a directed edge from <paramref name="from"/> to <paramref name="to"/>,
    /// meaning <paramref name="from"/> must precede <paramref name="to"/> in a
    /// topological order. Both nodes are added automatically if they are not already
    /// present.
    /// </summary>
    /// <param name="from">The edge's source node, which precedes <paramref name="to"/>.</param>
    /// <param name="to">The edge's target node, which depends on <paramref name="from"/>.</param>
    /// <returns><c>true</c> if the edge was newly added; <c>false</c> if it already existed.</returns>
    public bool AddEdge(TNode from, TNode to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        AddNode(from);
        AddNode(to);

        if (!_successorsByNode[from].Add(to))
        {
            return false;
        }

        _predecessorsByNode[to].Add(from);
        _edgeCount++;
        return true;
    }

    /// <summary>
    /// Determines whether <paramref name="node"/> exists in the graph.
    /// </summary>
    /// <param name="node">The node to look up.</param>
    /// <returns><c>true</c> if the node exists in the graph; otherwise, <c>false</c>.</returns>
    public bool ContainsNode(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return _nodes.Contains(node);
    }

    /// <summary>
    /// Gets the direct successors of <paramref name="node"/>, in the order their edges
    /// were added.
    /// </summary>
    /// <param name="node">The node whose successors to retrieve.</param>
    /// <returns>The direct successors of <paramref name="node"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="node"/> does not exist in the graph.</exception>
    /// <remarks>
    /// The returned list is a live view over this graph's internal edge storage, not a
    /// snapshot: further calls to <see cref="AddEdge"/> that add successors to
    /// <paramref name="node"/> are reflected in it. Copy it (for example with
    /// <c>ToList()</c>) before caching it across further mutations of the graph.
    /// </remarks>
    public IReadOnlyList<TNode> GetSuccessors(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_successorsByNode.TryGetValue(node, out var successors))
        {
            throw new ArgumentException(GraphErrorMessages.NodeNotInGraph, nameof(node));
        }

        return successors;
    }

    /// <summary>
    /// Gets the direct predecessors of <paramref name="node"/>, in the order their
    /// edges were added.
    /// </summary>
    /// <param name="node">The node whose predecessors to retrieve.</param>
    /// <returns>The direct predecessors of <paramref name="node"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="node"/> does not exist in the graph.</exception>
    /// <remarks>
    /// The returned list is a live view over this graph's internal edge storage, not a
    /// snapshot: further calls to <see cref="AddEdge"/> that add predecessors to
    /// <paramref name="node"/> are reflected in it. Copy it (for example with
    /// <c>ToList()</c>) before caching it across further mutations of the graph.
    /// </remarks>
    public IReadOnlyList<TNode> GetPredecessors(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_predecessorsByNode.TryGetValue(node, out var predecessors))
        {
            throw new ArgumentException(GraphErrorMessages.NodeNotInGraph, nameof(node));
        }

        return predecessors;
    }

    /// <summary>
    /// Computes a topological order over the graph using Kahn's algorithm, breaking
    /// ties between simultaneously-ready nodes by insertion order.
    /// </summary>
    /// <returns>
    /// A <see cref="TopologicalSortResult{TNode}"/> holding either the topological
    /// order, or the exact cycle that prevented one from existing.
    /// </returns>
    public TopologicalSortResult<TNode> TopologicalSort()
    {
        var inDegreeByNode = new Dictionary<TNode, int>(Comparer);
        var insertionIndexByNode = new Dictionary<TNode, int>(Comparer);
        var insertionIndex = 0;
        foreach (var node in _nodes)
        {
            inDegreeByNode[node] = _predecessorsByNode[node].Count;
            insertionIndexByNode[node] = insertionIndex++;
        }

        // Ready nodes are dequeued in insertion order, not readiness order: a
        // PriorityQueue keyed on each node's original insertion index guarantees that
        // whenever more than one node is simultaneously ready, the one added earliest to
        // the graph is emitted first, regardless of which became ready first.
        var ready = new PriorityQueue<TNode, int>();
        foreach (var node in _nodes)
        {
            if (inDegreeByNode[node] == 0)
            {
                ready.Enqueue(node, insertionIndexByNode[node]);
            }
        }

        var order = new List<TNode>(_nodes.Count);
        while (ready.Count > 0)
        {
            var node = ready.Dequeue();
            order.Add(node);

            foreach (var successor in _successorsByNode[node])
            {
                var remaining = --inDegreeByNode[successor];
                if (remaining == 0)
                {
                    ready.Enqueue(successor, insertionIndexByNode[successor]);
                }
            }
        }

        if (order.Count == _nodes.Count)
        {
            return TopologicalSortResult<TNode>.Acyclic(order);
        }

        var foundCycle = TryFindCycle(out var cycle);
        System.Diagnostics.Debug.Assert(foundCycle, "Kahn's algorithm stalled but no cycle was found by DFS.");
        return TopologicalSortResult<TNode>.Cyclic(cycle);
    }

    /// <summary>
    /// Searches the graph for a cycle using depth-first search with an explicit,
    /// caller-visible traversal stack, so the exact offending node sequence can be
    /// returned rather than just a yes/no answer.
    /// </summary>
    /// <param name="cycle">
    /// When this method returns <c>true</c>, the node sequence forming the cycle, in
    /// traversal order, not repeating the first node at the end. Otherwise an empty list.
    /// </param>
    /// <returns><c>true</c> if the graph contains a cycle; otherwise, <c>false</c>.</returns>
    public bool TryDetectCycle(out IReadOnlyList<TNode> cycle) => TryFindCycle(out cycle);

    /// <summary>
    /// Partitions the graph into its strongly connected components using Tarjan's
    /// algorithm, implemented iteratively with an explicit work stack so traversal
    /// depth is not bounded by the runtime call stack.
    /// </summary>
    /// <returns>
    /// Every strongly connected component in the graph. A node with no cycle through it
    /// forms a singleton component. Components are yielded in reverse topological order
    /// of the condensation graph; node order within a component reflects Tarjan's
    /// stack-pop order rather than insertion order.
    /// </returns>
    public IReadOnlyList<IReadOnlyList<TNode>> StronglyConnectedComponents()
    {
        var indexByNode = new Dictionary<TNode, int>(Comparer);
        var lowLinkByNode = new Dictionary<TNode, int>(Comparer);
        var onTarjanStack = new HashSet<TNode>(Comparer);
        var tarjanStack = new Stack<TNode>();
        var components = new List<IReadOnlyList<TNode>>();
        var workStack = new Stack<(TNode Node, int NextChildIndex)>();
        var nextIndex = 0;

        foreach (var root in _nodes)
        {
            if (indexByNode.ContainsKey(root))
            {
                continue;
            }

            workStack.Push((root, 0));

            while (workStack.Count > 0)
            {
                var (node, childIndex) = workStack.Pop();

                if (childIndex == 0)
                {
                    indexByNode[node] = nextIndex;
                    lowLinkByNode[node] = nextIndex;
                    nextIndex++;
                    tarjanStack.Push(node);
                    onTarjanStack.Add(node);
                }

                var successors = _successorsByNode[node];
                var recursedIntoChild = false;
                var i = childIndex;
                for (; i < successors.Count; i++)
                {
                    var child = successors[i];
                    if (!indexByNode.ContainsKey(child))
                    {
                        workStack.Push((node, i + 1));
                        workStack.Push((child, 0));
                        recursedIntoChild = true;
                        break;
                    }

                    if (onTarjanStack.Contains(child))
                    {
                        lowLinkByNode[node] = Math.Min(lowLinkByNode[node], indexByNode[child]);
                    }
                }

                if (recursedIntoChild)
                {
                    continue;
                }

                if (workStack.Count > 0)
                {
                    var parent = workStack.Peek().Node;
                    lowLinkByNode[parent] = Math.Min(lowLinkByNode[parent], lowLinkByNode[node]);
                }

                if (lowLinkByNode[node] != indexByNode[node])
                {
                    continue;
                }

                var component = new List<TNode>();
                TNode member;
                do
                {
                    member = tarjanStack.Pop();
                    onTarjanStack.Remove(member);
                    component.Add(member);
                }
                while (!Comparer.Equals(member, node));

                components.Add(component);
            }
        }

        return components;
    }

    private bool TryFindCycle(out IReadOnlyList<TNode> cycle)
    {
        var state = new Dictionary<TNode, VisitState>(Comparer);
        foreach (var node in _nodes)
        {
            state[node] = VisitState.Unvisited;
        }

        var path = new List<TNode>();
        var pathPositionByNode = new Dictionary<TNode, int>(Comparer);
        var workStack = new Stack<(TNode Node, int NextChildIndex)>();

        foreach (var start in _nodes)
        {
            if (state[start] != VisitState.Unvisited)
            {
                continue;
            }

            workStack.Push((start, 0));
            state[start] = VisitState.InProgress;
            path.Add(start);
            pathPositionByNode[start] = path.Count - 1;

            while (workStack.Count > 0)
            {
                var (node, childIndex) = workStack.Pop();
                var successors = _successorsByNode[node];
                var recursedIntoChild = false;
                var i = childIndex;
                for (; i < successors.Count; i++)
                {
                    var child = successors[i];
                    switch (state[child])
                    {
                        case VisitState.InProgress:
                            var cycleStart = pathPositionByNode[child];
                            cycle = path.GetRange(cycleStart, path.Count - cycleStart);
                            return true;
                        case VisitState.Unvisited:
                            workStack.Push((node, i + 1));
                            workStack.Push((child, 0));
                            state[child] = VisitState.InProgress;
                            path.Add(child);
                            pathPositionByNode[child] = path.Count - 1;
                            recursedIntoChild = true;
                            break;
                        case VisitState.Done:
                        default:
                            break;
                    }

                    if (recursedIntoChild)
                    {
                        break;
                    }
                }

                if (recursedIntoChild)
                {
                    continue;
                }

                state[node] = VisitState.Done;
                path.RemoveAt(path.Count - 1);
                pathPositionByNode.Remove(node);
            }
        }

        cycle = Array.Empty<TNode>();
        return false;
    }
}
