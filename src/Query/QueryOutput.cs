namespace ArturRios.Mediator.Query;

/// <summary>
/// Base type for the data payload returned by a query handler. Depending on the handler,
/// the payload is wrapped either in a <see cref="ArturRios.Output.DataOutput{T}"/> (single
/// result) or a <see cref="ArturRios.Output.PaginatedOutput{T}"/> (paginated collection).
/// </summary>
/// <remarks>
/// Derive from this type to describe the shape of the data returned by a query.
/// </remarks>
public abstract class QueryOutput;
