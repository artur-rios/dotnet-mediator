using ArturRios.Output;

namespace ArturRios.Mediator.Query.Interfaces;

/// <summary>
/// Handles a query asynchronously, returning a single typed result.
/// </summary>
/// <remarks>
/// Implement this interface for queries that return a single result and register the
/// implementation in the dependency injection container so the <see cref="QueryMediator"/>
/// can resolve it. For collection results with pagination metadata, use
/// <see cref="IPaginatedQueryHandlerAsync{TQuery, TOutput}"/> instead.
/// </remarks>
/// <typeparam name="TQuery">The query type handled by this handler.</typeparam>
/// <typeparam name="TOutput">The type of the data payload produced by this handler.</typeparam>
public interface IQueryHandlerAsync<in TQuery, TOutput>
    where TQuery : BaseQuery
    where TOutput : QueryOutput
{
    /// <summary>
    /// Executes the query asynchronously and returns its result.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <returns>
    /// A task that resolves to a <see cref="DataOutput{T}"/> carrying the result payload
    /// along with success state, messages and any errors produced while handling the query.
    /// </returns>
    Task<DataOutput<TOutput?>> HandleAsync(TQuery query);
}
