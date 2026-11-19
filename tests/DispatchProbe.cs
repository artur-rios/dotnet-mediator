using ArturRios.Mediator.Command.Interfaces;
using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Mediator.Tests.Command;
using ArturRios.Mediator.Tests.Query;
using ArturRios.Output;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests;

/// <summary>
/// The six entry points of the mediators, so a behaviour they all share is tested once, as a theory over them.
/// </summary>
public enum DispatchKind
{
    Command,
    CommandAsync,
    Query,
    QueryAsync,
    PaginatedQuery,
    PaginatedQueryAsync
}

/// <summary>
/// Dispatches an <see cref="EchoCommand"/> or a <see cref="SearchQuery"/> through
/// <see cref="CommandQueryMediator"/> by <see cref="DispatchKind"/>, and returns the value the handler echoed.
/// </summary>
public static class Dispatch
{
    public static TheoryData<DispatchKind> All => new(Enum.GetValues<DispatchKind>());

    public static TheoryData<DispatchKind> Asynchronous =>
        new(DispatchKind.CommandAsync, DispatchKind.QueryAsync, DispatchKind.PaginatedQueryAsync);

    public static bool IsAsynchronous(DispatchKind kind) =>
        kind is DispatchKind.CommandAsync or DispatchKind.QueryAsync or DispatchKind.PaginatedQueryAsync;

    /// <summary>
    /// The handler interface the mediator resolves for <paramref name="kind"/>.
    /// </summary>
    public static Type HandlerInterface(DispatchKind kind) => kind switch
    {
        DispatchKind.Command => typeof(ICommandHandler<EchoCommand, EchoCommandOutput>),
        DispatchKind.CommandAsync => typeof(ICommandHandlerAsync<EchoCommand, EchoCommandOutput>),
        DispatchKind.Query => typeof(IQueryHandler<SearchQuery, SearchQueryOutput>),
        DispatchKind.QueryAsync => typeof(IQueryHandlerAsync<SearchQuery, SearchQueryOutput>),
        DispatchKind.PaginatedQuery => typeof(IPaginatedQueryHandler<SearchQuery, SearchQueryOutput>),
        DispatchKind.PaginatedQueryAsync => typeof(IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    /// <summary>
    /// Dispatches a request carrying <paramref name="value"/>, or a null request when it is null. A synchronous
    /// entry point ignores <paramref name="cancellationToken"/>, since it takes none.
    /// </summary>
    /// <remarks>
    /// Like every await of these doubles, it ignores the caller's synchronization context, so a test that counts
    /// what is posted to one counts only what the mediator posts.
    /// </remarks>
    public static async Task<string?> Run(
        CommandQueryMediator mediator,
        DispatchKind kind,
        string? value,
        CancellationToken cancellationToken)
    {
        var command = value is null ? null : new EchoCommand { Value = value };
        var query = value is null ? null : new SearchQuery { Term = value };

        return kind switch
        {
            DispatchKind.Command =>
                mediator.ExecuteCommand<EchoCommand, EchoCommandOutput>(command!).Data?.Value,
            DispatchKind.CommandAsync =>
                (await mediator.ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(command!, cancellationToken)
                    .ConfigureAwait(false))
                .Data?.Value,
            DispatchKind.Query =>
                mediator.ExecuteQuery<SearchQuery, SearchQueryOutput>(query!).Data?.Value,
            DispatchKind.QueryAsync =>
                (await mediator.ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(query!, cancellationToken)
                    .ConfigureAwait(false))
                .Data?.Value,
            DispatchKind.PaginatedQuery =>
                Assert.Single(mediator.ExecutePaginatedQuery<SearchQuery, SearchQueryOutput>(query!).Data!).Value,
            DispatchKind.PaginatedQueryAsync =>
                Assert.Single((await mediator.ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(
                    query!, cancellationToken).ConfigureAwait(false)).Data!).Value,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }
}

/// <summary>
/// A scoped dependency of <see cref="ProbeHandler"/>, one per dispatch scope, that records how it was disposed.
/// </summary>
public sealed class TrackedResource(Func<Task> disposeAsyncGate) : IDisposable, IAsyncDisposable
{
    public bool DisposedSynchronously { get; private set; }

    public bool DisposedAsynchronously { get; private set; }

    public bool IsDisposed => DisposedSynchronously || DisposedAsynchronously;

    public void Dispose() => DisposedSynchronously = true;

    public async ValueTask DisposeAsync()
    {
        await disposeAsyncGate().ConfigureAwait(false);

        DisposedAsynchronously = true;
    }
}

/// <summary>
/// What every <see cref="ProbeHandler"/> of a provider does, and what it saw: set by the test before dispatching.
/// </summary>
public sealed class ProbeBehaviour
{
    private readonly List<TrackedResource> _created = [];

    /// <summary>Runs inside a synchronous handler, with the scope's resource.</summary>
    public Action<TrackedResource> OnHandle { get; set; } = _ => { };

    /// <summary>Runs inside an asynchronous handler, with the scope's resource and the token it received.</summary>
    public Func<TrackedResource, CancellationToken, Task> OnHandleAsync { get; set; } = (_, _) => Task.CompletedTask;

    /// <summary>Awaited by <see cref="TrackedResource.DisposeAsync"/> before it reports itself disposed.</summary>
    public Func<Task> DisposeAsyncGate { get; set; } = () => Task.CompletedTask;

    /// <summary>Whether each resource was still undisposed when its handler ran, in dispatch order.</summary>
    public List<bool> LiveDuringHandle { get; } = [];

    public IReadOnlyList<TrackedResource> Created
    {
        get
        {
            lock (_created)
            {
                return [.. _created];
            }
        }
    }

    public TrackedResource CreateResource()
    {
        var resource = new TrackedResource(() => DisposeAsyncGate());

        lock (_created)
        {
            _created.Add(resource);
        }

        return resource;
    }

    /// <summary>
    /// A provider with <see cref="CommandQueryMediator"/>, a scoped <see cref="TrackedResource"/> and a scoped
    /// <see cref="ProbeHandler"/> registered for every entry point.
    /// </summary>
    public ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<CommandQueryMediator>();
        services.AddSingleton(this);
        services.AddScoped(_ => CreateResource());
        services.AddScoped<ProbeHandler>();
        services.AddScoped<ICommandHandler<EchoCommand, EchoCommandOutput>>(Resolve);
        services.AddScoped<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>>(Resolve);
        services.AddScoped<IQueryHandler<SearchQuery, SearchQueryOutput>>(Resolve);
        services.AddScoped<IQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(Resolve);
        services.AddScoped<IPaginatedQueryHandler<SearchQuery, SearchQueryOutput>>(Resolve);
        services.AddScoped<IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(Resolve);

        return services.BuildServiceProvider();

        static ProbeHandler Resolve(IServiceProvider provider) => provider.GetRequiredService<ProbeHandler>();
    }
}

/// <summary>
/// One handler for every entry point: it runs the test's <see cref="ProbeBehaviour"/> with its scoped
/// <see cref="TrackedResource"/>, then echoes the request's value.
/// </summary>
public sealed class ProbeHandler(TrackedResource resource, ProbeBehaviour behaviour) :
    ICommandHandler<EchoCommand, EchoCommandOutput>,
    ICommandHandlerAsync<EchoCommand, EchoCommandOutput>,
    IQueryHandler<SearchQuery, SearchQueryOutput>,
    IQueryHandlerAsync<SearchQuery, SearchQueryOutput>,
    IPaginatedQueryHandler<SearchQuery, SearchQueryOutput>,
    IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>
{
    public DataOutput<EchoCommandOutput?> Handle(EchoCommand command)
    {
        Run();

        return DataOutput<EchoCommandOutput?>.New.WithData(new EchoCommandOutput { Value = command.Value });
    }

    public async Task<DataOutput<EchoCommandOutput?>> HandleAsync(
        EchoCommand command,
        CancellationToken cancellationToken = default)
    {
        await RunAsync(cancellationToken).ConfigureAwait(false);

        return DataOutput<EchoCommandOutput?>.New.WithData(new EchoCommandOutput { Value = command.Value });
    }

    DataOutput<SearchQueryOutput?> IQueryHandler<SearchQuery, SearchQueryOutput>.Handle(SearchQuery query)
    {
        Run();

        return DataOutput<SearchQueryOutput?>.New.WithData(new SearchQueryOutput { Value = query.Term });
    }

    async Task<DataOutput<SearchQueryOutput?>> IQueryHandlerAsync<SearchQuery, SearchQueryOutput>.HandleAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        await RunAsync(cancellationToken).ConfigureAwait(false);

        return DataOutput<SearchQueryOutput?>.New.WithData(new SearchQueryOutput { Value = query.Term });
    }

    PaginatedOutput<SearchQueryOutput> IPaginatedQueryHandler<SearchQuery, SearchQueryOutput>.Handle(SearchQuery query)
    {
        Run();

        return Page(query);
    }

    async Task<PaginatedOutput<SearchQueryOutput>> IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>.HandleAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        await RunAsync(cancellationToken).ConfigureAwait(false);

        return Page(query);
    }

    private static PaginatedOutput<SearchQueryOutput> Page(SearchQuery query)
    {
        var output = PaginatedOutput<SearchQueryOutput>.New.WithPagination(query.PageNumber, 1, 1);
        output.AddItem(new SearchQueryOutput { Value = query.Term });

        return output;
    }

    private void Run()
    {
        Record();
        behaviour.OnHandle(resource);
    }

    private Task RunAsync(CancellationToken cancellationToken)
    {
        Record();

        return behaviour.OnHandleAsync(resource, cancellationToken);
    }

    private void Record()
    {
        lock (behaviour.LiveDuringHandle)
        {
            behaviour.LiveDuringHandle.Add(!resource.IsDisposed);
        }
    }
}

/// <summary>
/// Counts the scopes a mediator opens. <c>CreateAsyncScope</c> is an extension over
/// <see cref="IServiceScopeFactory.CreateScope"/>, so the asynchronous entry points are counted too.
/// </summary>
public sealed class CountingScopeFactory(IServiceScopeFactory inner) : IServiceScopeFactory
{
    private int _scopesCreated;

    public int ScopesCreated => Volatile.Read(ref _scopesCreated);

    public IServiceScope CreateScope()
    {
        Interlocked.Increment(ref _scopesCreated);

        return inner.CreateScope();
    }
}
