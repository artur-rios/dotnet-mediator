namespace ArturRios.Mediator.Query;

/// <summary>
/// Base type for all queries. A query represents a read-only request for data and is
/// dispatched through the <see cref="QueryMediator"/> to its corresponding handler.
/// </summary>
/// <remarks>
/// Derive from this type to define a query, exposing any filter criteria as properties.
/// The <see cref="PageNumber"/> and <see cref="PageSize"/> properties are used by
/// paginated query handlers and ignored by non-paginated ones.
/// </remarks>
public abstract class BaseQuery
{
    /// <summary>
    /// The 1-based page number to retrieve when the query is executed as a paginated query.
    /// Defaults to <c>1</c>.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// The maximum number of items per page when the query is executed as a paginated query.
    /// Defaults to <c>100</c>.
    /// </summary>
    public int PageSize { get; set; } = 100;
}
