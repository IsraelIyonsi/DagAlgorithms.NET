using System.Runtime.ExceptionServices;

namespace DagAlgorithms.Net;

/// <summary>
/// Runs a per-node asynchronous action over a <see cref="DirectedGraph{TNode}"/>,
/// respecting every dependency edge, with a caller-bounded degree of parallelism.
/// </summary>
public static class GraphScheduler
{
    /// <summary>
    /// The smallest valid value for <c>maxDegreeOfParallelism</c> in
    /// <see cref="ExecuteAsync{TNode}"/>.
    /// </summary>
    public const int MinimumMaxDegreeOfParallelism = 1;

    /// <summary>
    /// Executes <paramref name="action"/> once for every node in <paramref name="graph"/>.
    /// A node's action starts only after every action for its direct predecessors (as
    /// added via <see cref="DirectedGraph{TNode}.AddEdge"/>) has completed successfully.
    /// No more than <paramref name="maxDegreeOfParallelism"/> actions run concurrently.
    /// </summary>
    /// <remarks>
    /// If any action throws, the exception is captured, every node action that has not
    /// yet started is cancelled via the <see cref="CancellationToken"/> passed to
    /// <paramref name="action"/>, and once every in-flight action has settled, the first
    /// captured exception is rethrown with its original stack trace preserved. Actions
    /// that do not observe cancellation cooperatively are allowed to run to completion;
    /// their results are discarded.
    /// </remarks>
    /// <typeparam name="TNode">The type used to identify nodes in the graph.</typeparam>
    /// <param name="graph">The dependency graph to execute.</param>
    /// <param name="action">
    /// The action to run for each node. Receives the node and a token that is
    /// cancelled as soon as another node's action fails.
    /// </param>
    /// <param name="maxDegreeOfParallelism">
    /// The maximum number of node actions allowed to run concurrently. Must be at least
    /// <see cref="MinimumMaxDegreeOfParallelism"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// A token used to cancel the whole execution from outside.
    /// </param>
    /// <returns>A task that completes when every node action has completed, or throws
    /// as described above.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxDegreeOfParallelism"/> is less than <see cref="MinimumMaxDegreeOfParallelism"/>.
    /// </exception>
    /// <exception cref="GraphCycleException{TNode}"><paramref name="graph"/> contains a cycle.</exception>
    public static async Task ExecuteAsync<TNode>(
        DirectedGraph<TNode> graph,
        Func<TNode, CancellationToken, Task> action,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
        where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(action);
        if (maxDegreeOfParallelism < MinimumMaxDegreeOfParallelism)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDegreeOfParallelism),
                maxDegreeOfParallelism,
                GraphErrorMessages.MaxDegreeOfParallelismMustBePositive);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var topology = graph.TopologicalSort();
        if (!topology.IsAcyclic)
        {
            throw new GraphCycleException<TNode>(topology.Cycle!);
        }

        var order = topology.Order!;
        if (order.Count == 0)
        {
            return;
        }

        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var exceptionGate = new object();
        Exception? firstException = null;

        async Task RunNodeAsync(TNode node, Task[] dependencies)
        {
            if (dependencies.Length > 0)
            {
                await Task.WhenAll(dependencies).ConfigureAwait(false);
            }

            await semaphore.WaitAsync(linkedCts.Token).ConfigureAwait(false);
            try
            {
                linkedCts.Token.ThrowIfCancellationRequested();
                await action(node, linkedCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !linkedCts.IsCancellationRequested)
            {
                lock (exceptionGate)
                {
                    firstException ??= ex;
                }

                linkedCts.Cancel();
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }

        var nodeTasks = new Dictionary<TNode, Task>(graph.Comparer);
        foreach (var node in order)
        {
            var dependencyTasks = graph.GetPredecessors(node).Select(p => nodeTasks[p]).ToArray();
            nodeTasks[node] = RunNodeAsync(node, dependencyTasks);
        }

        try
        {
            await Task.WhenAll(nodeTasks.Values).ConfigureAwait(false);
        }
        catch
        {
            // The authoritative failure is captured in firstException below; a faulted
            // dependency chain otherwise surfaces here too and would just be noise.
        }

        if (firstException is not null)
        {
            ExceptionDispatchInfo.Capture(firstException).Throw();
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
