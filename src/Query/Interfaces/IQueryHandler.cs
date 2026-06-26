using ArturRios.Output;

namespace ArturRios.Mediator.Query.Interfaces;

/// <summary>
/// Handles a query synchronously, returning a single typed result.
/// </summary>
/// <remarks>
/// Implement this interface for queries that return a single result and register the
/// implementation in the dependency injection container so the <see cref="QueryMediator"/>
/// can resolve it. For collection results with pagination metadata, use
/// <see cref="IPaginatedQueryHandler{TQuery, TOutput}"/> instead.
/// </remarks>
/// <typeparam name="TQuery">The query type handled by this handler.</typeparam>
/// <typeparam name="TOutput">The type of the data payload produced by this handler.</typeparam>
public interface IQueryHandler<in TQuery, TOutput>
    where TQuery : BaseQuery
    where TOutput : QueryOutput
{
    /// <summary>
    /// Executes the query and returns its result.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <returns>
    /// A <see cref="DataOutput{T}"/> carrying the result payload along with success state,
    /// messages and any errors produced while handling the query.
    /// </returns>
    DataOutput<TOutput?> Handle(TQuery query);
}
