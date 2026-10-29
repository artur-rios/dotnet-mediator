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
    /// Completes its disposal later, so whatever awaits it resumes through a continuation.
    /// </summary>
    private sealed class SlowAsyncOnlyResource : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => new(Task.Delay(20));
    }

    private sealed class CommandHandlerUsingSlowResource(SlowAsyncOnlyResource resource)
        : ICommandHandlerAsync<EchoCommand, EchoCommandOutput>
    {
        public Task<DataOutput<EchoCommandOutput?>> HandleAsync(
            EchoCommand command,
            CancellationToken cancellationToken = default)
        {
            Assert.NotNull(resource);

            return Task.FromResult(DataOutput<EchoCommandOutput?>.New.WithData(new EchoCommandOutput { Value = command.Value }));
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
            .ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" });

        Assert.Equal("x", output.Data?.Value);
        Assert.True(Assert.Single(created).Disposed);
    }

    [Fact]
    public async Task GivenAHandlerWithAnAsyncOnlyDisposableDependency_WhenAQueryIsDispatched_ThenTheScopeIsDisposedAsynchronously()
    {
        var created = new List<AsyncOnlyResource>();
        await using var provider = BuildProvider(created);

        var output = await provider.GetRequiredService<CommandQueryMediator>()
            .ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "x" });

        Assert.Equal("x", output.Data?.Value);
        Assert.True(Assert.Single(created).Disposed);
    }

    [Fact]
    public async Task GivenAHandlerWithAnAsyncOnlyDisposableDependency_WhenAPaginatedQueryIsDispatched_ThenTheScopeIsDisposedAsynchronously()
    {
        var created = new List<AsyncOnlyResource>();
        await using var provider = BuildProvider(created);

        var output = await provider.GetRequiredService<CommandQueryMediator>()
            .ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "x" });

        Assert.Equal("x", Assert.Single(output.Data!).Value);
        Assert.True(Assert.Single(created).Disposed);
    }

    [Fact]
    public async Task GivenACallerSynchronizationContext_WhenTheScopeIsDisposedAsynchronously_ThenTheContextIsNotCaptured()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandMediator>();
        services.AddScoped<SlowAsyncOnlyResource>();
        services.AddScoped<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>, CommandHandlerUsingSlowResource>();
        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<CommandMediator>();

        var context = new RecordingSynchronizationContext();
        var previous = SynchronizationContext.Current;
        Task<DataOutput<EchoCommandOutput?>> dispatch;

        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            dispatch = mediator.ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" });
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        var output = await dispatch;

        Assert.Equal("x", output.Data?.Value);
        Assert.Equal(0, context.Posts);
    }
}
