using ArturRios.Mediator.Query;
using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Output;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests.Query;

[Trait("Category", "Unit")]
public class QueryMediatorTests
{
    private static QueryMediator BuildMediator(IServiceProvider provider) =>
        new(provider.GetRequiredService<IServiceScopeFactory>());

    [Fact]
    public void GivenRegisteredHandler_WhenExecutingQuery_ThenResolvesAndInvokesHandler()
    {
        var handler = new SearchQueryHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<SearchQuery, SearchQueryOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var query = new SearchQuery { Term = "abc" };

        var result = mediator.ExecuteQuery<SearchQuery, SearchQueryOutput>(query);

        Assert.True(handler.WasCalled);
        Assert.Same(query, handler.ReceivedQuery);
        Assert.True(result.Success);
        Assert.Equal("abc", result.Data!.Value);
    }

    [Fact]
    public async Task GivenRegisteredAsyncHandler_WhenExecutingQueryAsync_ThenResolvesAndInvokesHandler()
    {
        var handler = new SearchQueryHandlerAsync();
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var query = new SearchQuery { Term = "def" };

        var result = await mediator.ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(
            query, TestContext.Current.CancellationToken);

        Assert.True(handler.WasCalled);
        Assert.Same(query, handler.ReceivedQuery);
        Assert.True(result.Success);
        Assert.Equal("def", result.Data!.Value);
    }

    [Fact]
    public void GivenRegisteredHandler_WhenExecutingPaginatedQuery_ThenResolvesAndInvokesHandler()
    {
        var handler = new SearchPaginatedQueryHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IPaginatedQueryHandler<SearchQuery, SearchQueryOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var query = new SearchQuery { Term = "page", PageNumber = 2 };

        var result = mediator.ExecutePaginatedQuery<SearchQuery, SearchQueryOutput>(query);

        Assert.True(handler.WasCalled);
        Assert.Same(query, handler.ReceivedQuery);
        Assert.Equal(2, result.PageNumber);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("page", result.Data[0].Value);
    }

    [Fact]
    public async Task GivenRegisteredAsyncHandler_WhenExecutingPaginatedQueryAsync_ThenResolvesAndInvokesHandler()
    {
        var handler = new SearchPaginatedQueryHandlerAsync();
        var services = new ServiceCollection();
        services.AddSingleton<IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var query = new SearchQuery { Term = "page", PageNumber = 3 };

        var result = await mediator.ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(
            query, TestContext.Current.CancellationToken);

        Assert.True(handler.WasCalled);
        Assert.Same(query, handler.ReceivedQuery);
        Assert.Equal(3, result.PageNumber);
        Assert.NotNull(result.Data);
        Assert.Single(result.Data);
        Assert.Equal("page", result.Data[0].Value);
    }

    [Fact]
    public void GivenNoRegisteredHandler_WhenExecutingQuery_ThenThrows()
    {
        var mediator = BuildMediator(new ServiceCollection().BuildServiceProvider());

        Assert.Throws<InvalidOperationException>(() =>
            mediator.ExecuteQuery<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "x" }));
    }

    [Fact]
    public void GivenNoRegisteredHandler_WhenExecutingPaginatedQuery_ThenThrows()
    {
        var mediator = BuildMediator(new ServiceCollection().BuildServiceProvider());

        Assert.Throws<InvalidOperationException>(() =>
            mediator.ExecutePaginatedQuery<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "x" }));
    }

    [Fact]
    public async Task GivenNoRegisteredHandler_WhenExecutingQueryAsync_ThenThrows()
    {
        var mediator = BuildMediator(new ServiceCollection().BuildServiceProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(
                new SearchQuery { Term = "x" }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GivenNoRegisteredHandler_WhenExecutingPaginatedQueryAsync_ThenThrows()
    {
        var mediator = BuildMediator(new ServiceCollection().BuildServiceProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(
                new SearchQuery { Term = "x" }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void GivenAHandlerThatFails_WhenExecutingQuery_ThenReturnsTheHandlerOutputAsItIs()
    {
        var failed = DataOutput<SearchQueryOutput?>.New.WithError("query handler failed");
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<SearchQuery, SearchQueryOutput>>(new FixedQueryHandler(failed));
        var mediator = BuildMediator(services.BuildServiceProvider());

        var result = mediator.ExecuteQuery<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "x" });

        Assert.Same(failed, result);
        Assert.False(result.Success);
    }

    [Fact]
    public void GivenAPaginatedHandler_WhenExecutingPaginatedQuery_ThenItsPaginationMetadataComesBackAsItIs()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IPaginatedQueryHandler<SearchQuery, SearchQueryOutput>>(new SearchPaginatedQueryHandler());
        var mediator = BuildMediator(services.BuildServiceProvider());

        var result = mediator.ExecutePaginatedQuery<SearchQuery, SearchQueryOutput>(
            new SearchQuery { Term = "x", PageNumber = 4, PageSize = 7 });

        // The mediator neither applies nor checks the requested page: the handler's metadata is returned.
        Assert.Equal(4, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(1, result.TotalItems);
    }

    [Fact]
    public void GivenANewQuery_WhenNoPageIsSet_ThenItAsksForTheFirstPageOfAHundredItems()
    {
        var query = new SearchQuery { Term = "x" };

        Assert.Equal(1, query.PageNumber);
        Assert.Equal(100, query.PageSize);
    }

    private sealed class FixedQueryHandler(DataOutput<SearchQueryOutput?> output)
        : IQueryHandler<SearchQuery, SearchQueryOutput>
    {
        public DataOutput<SearchQueryOutput?> Handle(SearchQuery query) => output;
    }
}
