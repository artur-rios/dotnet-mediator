using ArturRios.Mediator.Command;
using ArturRios.Mediator.Command.Interfaces;
using ArturRios.Mediator.Query;
using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Mediator.Tests.Command;
using ArturRios.Mediator.Tests.Query;
using ArturRios.Output;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests;

/// <summary>
/// The asynchronous entry points dispose the per-dispatch scope asynchronously, so a handler may depend on a
/// scoped service that only implements <see cref="IAsyncDisposable"/>. Disposing that scope synchronously throws
/// <see cref="InvalidOperationException"/> after the handler has already run.
/// </summary>
[Trait("Category", "Unit")]
public class ScopeDisposalTests
{
    private sealed class AsyncOnlyResource : IAsyncDisposable
    {
        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;

            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Records every continuation posted to it instead of running it on a dedicated thread.
    /// </summary>
    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        public int Posts;

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref Posts);
            base.Post(d, state);
        }
    }

    private sealed class CommandHandlerUsingAsyncResource(AsyncOnlyResource resource)
        : ICommandHandlerAsync<EchoCommand, EchoCommandOutput>
    {
        public Task<DataOutput<EchoCommandOutput?>> HandleAsync(
            EchoCommand command,
            CancellationToken cancellationToken = default)
        {
            Assert.False(resource.Disposed);

            return Task.FromResult(DataOutput<EchoCommandOutput?>.New.WithData(new EchoCommandOutput { Value = command.Value }));
        }
    }

    private sealed class QueryHandlerUsingAsyncResource(AsyncOnlyResource resource)
        : IQueryHandlerAsync<SearchQuery, SearchQueryOutput>
    {
        public Task<DataOutput<SearchQueryOutput?>> HandleAsync(
            SearchQuery query,
            CancellationToken cancellationToken = default)
        {
            Assert.False(resource.Disposed);

            return Task.FromResult(DataOutput<SearchQueryOutput?>.New.WithData(new SearchQueryOutput { Value = query.Term }));
        }
    }

    private sealed class PaginatedQueryHandlerUsingAsyncResource(AsyncOnlyResource resource)
        : IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>
    {
        public Task<PaginatedOutput<SearchQueryOutput>> HandleAsync(
            SearchQuery query,
            CancellationToken cancellationToken = default)
        {
            Assert.False(resource.Disposed);

            var output = PaginatedOutput<SearchQueryOutput>.New.WithPagination(query.PageNumber, 1, 1);
            output.AddItem(new SearchQueryOutput { Value = query.Term });

            return Task.FromResult(output);
        }
    }

    private static ServiceProvider BuildProvider(List<AsyncOnlyResource> created)
    {
        var services = new ServiceCollection();

        services.AddSingleton<CommandQueryMediator>();
        services.AddScoped(_ =>
        {
            var resource = new AsyncOnlyResource();
            created.Add(resource);

            return resource;
        });
        services.AddScoped<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>, CommandHandlerUsingAsyncResource>();
        services.AddScoped<IQueryHandlerAsync<SearchQuery, SearchQueryOutput>, QueryHandlerUsingAsyncResource>();
        services.AddScoped<IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>, PaginatedQueryHandlerUsingAsyncResource>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GivenAHandlerWithAnAsyncOnlyDisposableDependency_WhenACommandIsDispatched_ThenTheScopeIsDisposedAsynchronously()
    {
        var created = new List<AsyncOnlyResource>();
        await using var provider = BuildProvider(created);

        var output = await provider.GetRequiredService<CommandQueryMediator>()
            .ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(
                new EchoCommand { Value = "x" }, TestContext.Current.CancellationToken);

        Assert.Equal("x", output.Data?.Value);
        Assert.True(Assert.Single(created).Disposed);
    }

    [Fact]
    public async Task GivenAHandlerWithAnAsyncOnlyDisposableDependency_WhenAQueryIsDispatched_ThenTheScopeIsDisposedAsynchronously()
    {
        var created = new List<AsyncOnlyResource>();
        await using var provider = BuildProvider(created);

        var output = await provider.GetRequiredService<CommandQueryMediator>()
            .ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(
                new SearchQuery { Term = "x" }, TestContext.Current.CancellationToken);

        Assert.Equal("x", output.Data?.Value);
        Assert.True(Assert.Single(created).Disposed);
    }

    [Fact]
    public async Task GivenAHandlerWithAnAsyncOnlyDisposableDependency_WhenAPaginatedQueryIsDispatched_ThenTheScopeIsDisposedAsynchronously()
    {
        var created = new List<AsyncOnlyResource>();
        await using var provider = BuildProvider(created);

        var output = await provider.GetRequiredService<CommandQueryMediator>()
            .ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(
                new SearchQuery { Term = "x" }, TestContext.Current.CancellationToken);

        Assert.Equal("x", Assert.Single(output.Data!).Value);
        Assert.True(Assert.Single(created).Disposed);
    }

    [Theory]
    [MemberData(nameof(Dispatch.Asynchronous), MemberType = typeof(Dispatch))]
    public async Task GivenACallerSynchronizationContext_WhenTheScopeIsDisposedAsynchronously_ThenTheContextIsNotCaptured(
        DispatchKind kind)
    {
        // The scope's disposal completes only when the test lets it, after the caller's context is gone, so the
        // dispatch resumes through a continuation: posted to that context unless the await ignores it.
        var disposal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var behaviour = new ProbeBehaviour { DisposeAsyncGate = () => disposal.Task };
        await using var provider = behaviour.BuildProvider();
        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        var context = new RecordingSynchronizationContext();
        var previous = SynchronizationContext.Current;
        Task<string?> dispatch;

        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            dispatch = Dispatch.Run(mediator, kind, "x", TestContext.Current.CancellationToken);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.False(dispatch.IsCompleted);

        disposal.SetResult();

        Assert.Equal("x", await dispatch);
        Assert.True(Assert.Single(behaviour.Created).DisposedAsynchronously);
        Assert.Equal(0, context.Posts);
    }

    [Theory]
    [MemberData(nameof(Dispatch.All), MemberType = typeof(Dispatch))]
    public async Task GivenAScopedDependency_WhenDispatched_ThenItIsLiveDuringTheHandlerAndDisposedAfter(DispatchKind kind)
    {
        var behaviour = new ProbeBehaviour();
        await using var provider = behaviour.BuildProvider();

        var echoed = await Dispatch.Run(provider.GetRequiredService<CommandQueryMediator>(), kind, "x",
            TestContext.Current.CancellationToken);

        var resource = Assert.Single(behaviour.Created);
        Assert.Equal("x", echoed);
        Assert.True(Assert.Single(behaviour.LiveDuringHandle));
        Assert.True(resource.IsDisposed);
        // The synchronous entry points dispose the scope synchronously, the asynchronous ones asynchronously.
        Assert.Equal(Dispatch.IsAsynchronous(kind), resource.DisposedAsynchronously);
        Assert.Equal(!Dispatch.IsAsynchronous(kind), resource.DisposedSynchronously);
    }

    [Theory]
    [MemberData(nameof(Dispatch.All), MemberType = typeof(Dispatch))]
    public async Task GivenTwoDispatches_WhenEachResolvesAScopedDependency_ThenEachGetsItsOwnScope(DispatchKind kind)
    {
        var behaviour = new ProbeBehaviour();
        await using var provider = behaviour.BuildProvider();
        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        await Dispatch.Run(mediator, kind, "a", TestContext.Current.CancellationToken);
        await Dispatch.Run(mediator, kind, "b", TestContext.Current.CancellationToken);

        Assert.Equal(2, behaviour.Created.Count);
        Assert.NotSame(behaviour.Created[0], behaviour.Created[1]);
        Assert.All(behaviour.LiveDuringHandle, Assert.True);
    }

    [Theory]
    [MemberData(nameof(Dispatch.Asynchronous), MemberType = typeof(Dispatch))]
    public async Task GivenOverlappingDispatches_WhenAllAreInFlightAtOnce_ThenEachHasItsOwnScopeAndResult(DispatchKind kind)
    {
        const int dispatches = 8;
        var allEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = 0;
        var behaviour = new ProbeBehaviour
        {
            // Every handler waits until all of them are running, so none finishes before the last one starts.
            OnHandleAsync = async (_, cancellationToken) =>
            {
                if (Interlocked.Increment(ref entered) == dispatches)
                {
                    allEntered.SetResult();
                }

                // The limit only turns a dispatch that blocks the next one into a failure instead of a hang.
                await allEntered.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
        };
        await using var provider = behaviour.BuildProvider();
        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        var values = Enumerable.Range(0, dispatches).Select(index => $"value {index}").ToList();
        var echoed = await Task.WhenAll(values.Select(value =>
            Task.Run(() => Dispatch.Run(mediator, kind, value, TestContext.Current.CancellationToken),
                TestContext.Current.CancellationToken)));

        Assert.Equal(values, echoed);
        Assert.Equal(dispatches, behaviour.Created.Distinct().Count());
        Assert.All(behaviour.Created, resource => Assert.True(resource.DisposedAsynchronously));
        Assert.All(behaviour.LiveDuringHandle, Assert.True);
    }
}
