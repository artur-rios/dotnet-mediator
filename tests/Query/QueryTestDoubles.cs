using ArturRios.Mediator.Query;
using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Output;

namespace ArturRios.Mediator.Tests.Query;

/// <summary>
/// Query that asks for items matching a given term.
/// </summary>
public class SearchQuery : BaseQuery
{
    public required string Term { get; init; }
}

/// <summary>
/// Output produced by the query handlers.
/// </summary>
public class SearchQueryOutput : QueryOutput
{
    public required string Value { get; init; }
}

/// <summary>
/// Synchronous query handler returning a single data payload.
/// </summary>
public class SearchQueryHandler : IQueryHandler<SearchQuery, SearchQueryOutput>
{
    public bool WasCalled { get; private set; }
    public SearchQuery? ReceivedQuery { get; private set; }

    public DataOutput<SearchQueryOutput?> Handle(SearchQuery query)
    {
        WasCalled = true;
        ReceivedQuery = query;

        return DataOutput<SearchQueryOutput?>.New.WithData(new SearchQueryOutput { Value = query.Term });
    }
}

/// <summary>
/// Asynchronous query handler returning a single data payload.
/// </summary>
public class SearchQueryHandlerAsync : IQueryHandlerAsync<SearchQuery, SearchQueryOutput>
{
    public bool WasCalled { get; private set; }
    public SearchQuery? ReceivedQuery { get; private set; }

    public async Task<DataOutput<SearchQueryOutput?>> HandleAsync(SearchQuery query)
    {
        await Task.Yield();

        WasCalled = true;
        ReceivedQuery = query;

        return DataOutput<SearchQueryOutput?>.New.WithData(new SearchQueryOutput { Value = query.Term });
    }
}

/// <summary>
/// Synchronous paginated query handler echoing the requested page metadata.
/// </summary>
public class SearchPaginatedQueryHandler : IPaginatedQueryHandler<SearchQuery, SearchQueryOutput>
{
    public bool WasCalled { get; private set; }
    public SearchQuery? ReceivedQuery { get; private set; }

    public PaginatedOutput<SearchQueryOutput> Handle(SearchQuery query)
    {
        WasCalled = true;
        ReceivedQuery = query;

        var output = PaginatedOutput<SearchQueryOutput>.New.WithPagination(query.PageNumber, 1);
        output.AddItem(new SearchQueryOutput { Value = query.Term });

        return output;
    }
}

/// <summary>
/// Asynchronous paginated query handler echoing the requested page metadata.
/// </summary>
public class SearchPaginatedQueryHandlerAsync : IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>
{
    public bool WasCalled { get; private set; }
    public SearchQuery? ReceivedQuery { get; private set; }

    public async Task<PaginatedOutput<SearchQueryOutput>> HandleAsync(SearchQuery query)
    {
        await Task.Yield();

        WasCalled = true;
        ReceivedQuery = query;

        var output = PaginatedOutput<SearchQueryOutput>.New.WithPagination(query.PageNumber, 1);
        output.AddItem(new SearchQueryOutput { Value = query.Term });

        return output;
    }
}
