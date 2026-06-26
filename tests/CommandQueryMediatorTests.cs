using ArturRios.Mediator.Command.Interfaces;
using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Mediator.Tests.Command;
using ArturRios.Mediator.Tests.Query;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests;

public class CommandQueryMediatorTests
{
    private static CommandQueryMediator BuildMediator(IServiceProvider provider) =>
        new(provider.GetRequiredService<IServiceScopeFactory>());

    [Fact]
    public void GivenRegisteredCommandHandler_WhenExecutingCommand_ThenDelegatesToCommandMediator()
    {
        var handler = new EchoCommandHandler();
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<EchoCommand, EchoCommandOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var result = mediator.ExecuteCommand<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "cmd" });

        Assert.True(handler.WasCalled);
        Assert.Equal("cmd", result.Data!.Value);
    }

    [Fact]
    public async Task GivenRegisteredAsyncCommandHandler_WhenExecutingCommandAsync_ThenDelegatesToCommandMediator()
    {
        var handler = new EchoCommandHandlerAsync();
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var result = await mediator.ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "cmd" });

        Assert.True(handler.WasCalled);
        Assert.Equal("cmd", result.Data!.Value);
    }

    [Fact]
    public void GivenRegisteredQueryHandler_WhenExecutingQuery_ThenDelegatesToQueryMediator()
    {
        var handler = new SearchQueryHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandler<SearchQuery, SearchQueryOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var result = mediator.ExecuteQuery<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "qry" });

        Assert.True(handler.WasCalled);
        Assert.Equal("qry", result.Data!.Value);
    }

    [Fact]
    public async Task GivenRegisteredAsyncQueryHandler_WhenExecutingQueryAsync_ThenDelegatesToQueryMediator()
    {
        var handler = new SearchQueryHandlerAsync();
        var services = new ServiceCollection();
        services.AddSingleton<IQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var result = await mediator.ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "qry" });

        Assert.True(handler.WasCalled);
        Assert.Equal("qry", result.Data!.Value);
    }

    [Fact]
    public void GivenRegisteredPaginatedQueryHandler_WhenExecutingPaginatedQuery_ThenDelegatesToQueryMediator()
    {
        var handler = new SearchPaginatedQueryHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IPaginatedQueryHandler<SearchQuery, SearchQueryOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var result = mediator.ExecutePaginatedQuery<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "qry" });

        Assert.True(handler.WasCalled);
        Assert.Single(result.Data);
        Assert.Equal("qry", result.Data[0].Value);
    }

    [Fact]
    public async Task GivenRegisteredAsyncPaginatedQueryHandler_WhenExecutingPaginatedQueryAsync_ThenDelegatesToQueryMediator()
    {
        var handler = new SearchPaginatedQueryHandlerAsync();
        var services = new ServiceCollection();
        services.AddSingleton<IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var result = await mediator.ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "qry" });

        Assert.True(handler.WasCalled);
        Assert.Single(result.Data);
        Assert.Equal("qry", result.Data[0].Value);
    }
}
