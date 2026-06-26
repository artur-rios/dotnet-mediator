using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Output;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Query;

/// <summary>
/// Dispatches queries to their registered handlers. For each call a new dependency
/// injection scope is created, the matching handler is resolved from it, and the
/// handler is invoked. Supports both single-result and paginated queries.
/// </summary>
/// <remarks>
/// Register this mediator and the query handlers in your dependency injection container.
/// Because a fresh scope is created per execution, scoped dependencies of a handler
/// (such as a database context) are isolated to a single query execution.
/// </remarks>
/// <param name="scopeFactory">
/// The factory used to create a dependency injection scope for resolving handlers.
/// </param>
public class QueryMediator(IServiceScopeFactory scopeFactory)
{
    /// <summary>
    /// Resolves the synchronous paginated handler for the given query and executes it.
    /// </summary>
    /// <typeparam name="TQuery">The query type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the items in the paginated result.</typeparam>
    /// <param name="query">The query instance to execute, including the requested page metadata.</param>
    /// <returns>The <see cref="PaginatedOutput{T}"/> returned by the resolved handler.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IPaginatedQueryHandler{TQuery, TOutput}"/> is registered for the
    /// requested query and output types.
    /// </exception>
    public PaginatedOutput<TOutput> ExecutePaginatedQuery<TQuery, TOutput>(TQuery query)
        where TQuery : BaseQuery
        where TOutput : QueryOutput
    {
        using var scoped = scopeFactory.CreateScope();

        var handler = scoped.ServiceProvider.GetRequiredService<IPaginatedQueryHandler<TQuery, TOutput>>();

        return handler.Handle(query);
    }

    /// <summary>
    /// Resolves the asynchronous paginated handler for the given query and executes it.
    /// </summary>
    /// <typeparam name="TQuery">The query type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the items in the paginated result.</typeparam>
    /// <param name="query">The query instance to execute, including the requested page metadata.</param>
    /// <returns>
    /// A task that resolves to the <see cref="PaginatedOutput{T}"/> returned by the resolved handler.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IPaginatedQueryHandlerAsync{TQuery, TOutput}"/> is registered for the
    /// requested query and output types.
    /// </exception>
    public async Task<PaginatedOutput<TOutput>> ExecutePaginatedQueryAsync<TQuery, TOutput>(TQuery query)
        where TQuery : BaseQuery
        where TOutput : QueryOutput
    {
        using var scoped = scopeFactory.CreateScope();

        var handler = scoped.ServiceProvider.GetRequiredService<IPaginatedQueryHandlerAsync<TQuery, TOutput>>();

        return await handler.HandleAsync(query);
    }

    /// <summary>
    /// Resolves the synchronous single-result handler for the given query and executes it.
    /// </summary>
    /// <typeparam name="TQuery">The query type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the data payload produced by the handler.</typeparam>
    /// <param name="query">The query instance to execute.</param>
    /// <returns>The <see cref="DataOutput{T}"/> returned by the resolved handler.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IQueryHandler{TQuery, TOutput}"/> is registered for the
    /// requested query and output types.
    /// </exception>
    public DataOutput<TOutput?> ExecuteQuery<TQuery, TOutput>(TQuery query)
        where TQuery : BaseQuery
        where TOutput : QueryOutput
    {
        using var scoped = scopeFactory.CreateScope();

        var handler = scoped.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TOutput>>();

        return handler.Handle(query);
    }

    /// <summary>
    /// Resolves the asynchronous single-result handler for the given query and executes it.
    /// </summary>
    /// <typeparam name="TQuery">The query type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the data payload produced by the handler.</typeparam>
    /// <param name="query">The query instance to execute.</param>
    /// <returns>
    /// A task that resolves to the <see cref="DataOutput{T}"/> returned by the resolved handler.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IQueryHandlerAsync{TQuery, TOutput}"/> is registered for the
    /// requested query and output types.
    /// </exception>
    public async Task<DataOutput<TOutput?>> ExecuteQueryAsync<TQuery, TOutput>(TQuery query)
        where TQuery : BaseQuery
        where TOutput : QueryOutput
    {
        using var scoped = scopeFactory.CreateScope();

        var handler = scoped.ServiceProvider.GetRequiredService<IQueryHandlerAsync<TQuery, TOutput>>();

        return await handler.HandleAsync(query);
    }
}
