using ArturRios.Mediator.Query;
using ArturRios.Mediator.Query.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests.Query;

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

        var result = await mediator.ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(query);

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

        var result = await mediator.ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(query);

        Assert.True(handler.WasCalled);
        Assert.Same(query, handler.ReceivedQuery);
        Assert.Equal(3, result.PageNumber);
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
}
