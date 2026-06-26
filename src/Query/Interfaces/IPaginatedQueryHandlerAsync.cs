using ArturRios.Output;

namespace ArturRios.Mediator.Query.Interfaces;

/// <summary>
/// Handles a query asynchronously, returning a paginated collection of results.
/// </summary>
/// <remarks>
/// Implement this interface for queries that return a page of results together with
/// pagination metadata, and register the implementation in the dependency injection
/// container so the <see cref="QueryMediator"/> can resolve it. The requested page is
/// described by <see cref="BaseQuery.PageNumber"/> and <see cref="BaseQuery.PageSize"/>.
/// </remarks>
/// <typeparam name="TQuery">The query type handled by this handler.</typeparam>
/// <typeparam name="TOutput">The type of the items in the paginated result.</typeparam>
public interface IPaginatedQueryHandlerAsync<in TQuery, TOutput> where TQuery : BaseQuery where TOutput : QueryOutput
{
    /// <summary>
    /// Executes the query asynchronously and returns a page of results.
    /// </summary>
    /// <param name="query">The query to execute, including the requested page metadata.</param>
    /// <returns>
    /// A task that resolves to a <see cref="PaginatedOutput{T}"/> carrying the page of results
    /// along with pagination metadata, success state, messages and any errors produced while
    /// handling the query.
    /// </returns>
    Task<PaginatedOutput<TOutput>> HandleAsync(TQuery query);
}
