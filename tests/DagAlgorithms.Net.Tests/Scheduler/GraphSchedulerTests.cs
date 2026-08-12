using System.Collections.Concurrent;

namespace DagAlgorithms.Net.Tests.Scheduler;

public class GraphSchedulerTests
{
    [Fact]
    public async Task ExecuteAsync_never_starts_a_node_before_its_dependencies_have_completed()
    {
        var graph = GraphFixtures.Diamond();
        var started = new Dictionary<string, TaskCompletionSource>(graph.Nodes.Count);
        var release = new Dictionary<string, TaskCompletionSource>(graph.Nodes.Count);
        foreach (var node in graph.Nodes)
        {
            started[node] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            release[node] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        async Task Action(string node, CancellationToken ct)
        {
            started[node].SetResult();
            await release[node].Task.WaitAsync(ct);
        }

        var executeTask = GraphScheduler.ExecuteAsync(graph, Action, maxDegreeOfParallelism: 4);

        await started["A"].Task;
        Assert.False(started["B"].Task.IsCompleted);
        Assert.False(started["C"].Task.IsCompleted);
        Assert.False(started["D"].Task.IsCompleted);

        release["A"].SetResult();
        await Task.WhenAll(started["B"].Task, started["C"].Task);
        Assert.False(started["D"].Task.IsCompleted);

        release["B"].SetResult();
        release["C"].SetResult();
        await started["D"].Task;

        release["D"].SetResult();
        await executeTask;
    }

    [Fact]
    public async Task ExecuteAsync_never_invokes_the_action_for_a_dependent_of_a_failed_node()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("Fail", "Child");
        graph.AddEdge("Child", "Grandchild");

        var invoked = new ConcurrentBag<string>();
        var expectedException = new InvalidOperationException("boom");

        Task Action(string node, CancellationToken ct)
        {
            invoked.Add(node);
            if (node == "Fail")
            {
                throw expectedException;
            }

            return Task.CompletedTask;
        }

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => GraphScheduler.ExecuteAsync(graph, Action, maxDegreeOfParallelism: 3));

        Assert.Same(expectedException, actual);
        Assert.Equal(new[] { "Fail" }, invoked.ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_bounds_concurrency_to_maxDegreeOfParallelism()
    {
        const int MaxDegreeOfParallelism = 2;
        const int IndependentNodeCount = 4;

        var graph = new DirectedGraph<string>();
        for (var i = 0; i < IndependentNodeCount; i++)
        {
            graph.AddNode($"N{i}");
        }

        var currentConcurrency = 0;
        var maxObservedConcurrency = 0;
        var gate = new object();
        var arrivedCount = 0;
        var allArrivedAtCapacityTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task Action(string node, CancellationToken ct)
        {
            _ = node;
            lock (gate)
            {
                currentConcurrency++;
                maxObservedConcurrency = Math.Max(maxObservedConcurrency, currentConcurrency);
            }

            if (Interlocked.Increment(ref arrivedCount) % MaxDegreeOfParallelism == 0)
            {
                allArrivedAtCapacityTcs.TrySetResult();
            }

            await allArrivedAtCapacityTcs.Task.WaitAsync(ct);

            lock (gate)
            {
                currentConcurrency--;
            }
        }

        await GraphScheduler.ExecuteAsync(graph, Action, MaxDegreeOfParallelism);

        Assert.Equal(MaxDegreeOfParallelism, maxObservedConcurrency);
    }

    [Fact]
    public async Task ExecuteAsync_surfaces_the_first_failure_and_cancels_remaining_nodes()
    {
        const int NodeCount = 3;
        var graph = new DirectedGraph<string>();
        graph.AddNode("Fail");
        graph.AddNode("Slow1");
        graph.AddNode("Slow2");

        var expectedException = new InvalidOperationException("boom");
        var startedCount = 0;
        var allStartedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedCancellation = new ConcurrentDictionary<string, bool>();

        async Task Action(string node, CancellationToken ct)
        {
            if (Interlocked.Increment(ref startedCount) == NodeCount)
            {
                allStartedTcs.TrySetResult();
            }

            await allStartedTcs.Task;

            if (node == "Fail")
            {
                throw expectedException;
            }

            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException)
            {
                observedCancellation[node] = true;
                throw;
            }
        }

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => GraphScheduler.ExecuteAsync(graph, Action, maxDegreeOfParallelism: NodeCount));

        Assert.Same(expectedException, actual);
        Assert.True(observedCancellation.GetValueOrDefault("Slow1"));
        Assert.True(observedCancellation.GetValueOrDefault("Slow2"));
    }

    [Fact]
    public async Task ExecuteAsync_throws_GraphCycleException_and_runs_no_actions_for_a_cyclic_graph()
    {
        var graph = GraphFixtures.SimpleCycle();
        var invocationCount = 0;

        Task Action(string node, CancellationToken ct)
        {
            _ = node;
            _ = ct;
            Interlocked.Increment(ref invocationCount);
            return Task.CompletedTask;
        }

        var exception = await Assert.ThrowsAsync<GraphCycleException<string>>(
            () => GraphScheduler.ExecuteAsync(graph, Action, maxDegreeOfParallelism: 1));

        Assert.Equal(new[] { "A", "B", "C" }, exception.Cycle);
        Assert.Equal(0, invocationCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ExecuteAsync_throws_for_a_non_positive_maxDegreeOfParallelism(int invalidValue)
    {
        var graph = GraphFixtures.SingleIsolatedNode();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => GraphScheduler.ExecuteAsync(graph, (_, _) => Task.CompletedTask, invalidValue));
    }

    [Fact]
    public async Task ExecuteAsync_throws_for_a_null_graph()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => GraphScheduler.ExecuteAsync<string>(null!, (_, _) => Task.CompletedTask, 1));
    }

    [Fact]
    public async Task ExecuteAsync_throws_for_a_null_action()
    {
        var graph = GraphFixtures.SingleIsolatedNode();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => GraphScheduler.ExecuteAsync(graph, null!, 1));
    }

    [Fact]
    public async Task ExecuteAsync_throws_immediately_for_an_already_cancelled_token_and_runs_no_actions()
    {
        var graph = GraphFixtures.SingleIsolatedNode();
        var invocationCount = 0;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Task Action(string node, CancellationToken ct)
        {
            _ = node;
            _ = ct;
            Interlocked.Increment(ref invocationCount);
            return Task.CompletedTask;
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => GraphScheduler.ExecuteAsync(graph, Action, maxDegreeOfParallelism: 1, cts.Token));

        Assert.Equal(0, invocationCount);
    }

    [Fact]
    public async Task ExecuteAsync_completes_without_invoking_any_action_for_an_empty_graph()
    {
        var graph = GraphFixtures.Empty();
        var invocationCount = 0;

        Task Action(string node, CancellationToken ct)
        {
            _ = node;
            _ = ct;
            Interlocked.Increment(ref invocationCount);
            return Task.CompletedTask;
        }

        await GraphScheduler.ExecuteAsync(graph, Action, maxDegreeOfParallelism: 1);

        Assert.Equal(0, invocationCount);
    }

    [Fact]
    public async Task ExecuteAsync_invokes_a_single_isolated_node_exactly_once()
    {
        var graph = GraphFixtures.SingleIsolatedNode();
        var invocationCount = 0;

        Task Action(string node, CancellationToken ct)
        {
            _ = node;
            _ = ct;
            Interlocked.Increment(ref invocationCount);
            return Task.CompletedTask;
        }

        await GraphScheduler.ExecuteAsync(graph, Action, maxDegreeOfParallelism: 1);

        Assert.Equal(1, invocationCount);
    }
}
