using ArturRios.Mediator.Command.Interfaces;
using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Mediator.Tests.Command;
using ArturRios.Mediator.Tests.Query;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests;

/// <summary>
/// A null command or query is rejected at dispatch, before a scope is created or a handler resolved, instead of
/// reaching the handler as a null argument.
/// </summary>
[Trait("Category", "Unit")]
public class NullArgumentTests
{
    private readonly EchoCommandHandler _commandHandler = new();
    private readonly EchoCommandHandlerAsync _commandHandlerAsync = new();
    private readonly SearchQueryHandler _queryHandler = new();
    private readonly SearchQueryHandlerAsync _queryHandlerAsync = new();
    private readonly SearchPaginatedQueryHandler _paginatedHandler = new();
    private readonly SearchPaginatedQueryHandlerAsync _paginatedHandlerAsync = new();

    private CommandQueryMediator BuildMediator()
    {
        var services = new ServiceCollection();

        services.AddSingleton<CommandQueryMediator>();
        services.AddSingleton<ICommandHandler<EchoCommand, EchoCommandOutput>>(_commandHandler);
        services.AddSingleton<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>>(_commandHandlerAsync);
        services.AddSingleton<IQueryHandler<SearchQuery, SearchQueryOutput>>(_queryHandler);
        services.AddSingleton<IQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(_queryHandlerAsync);
        services.AddSingleton<IPaginatedQueryHandler<SearchQuery, SearchQueryOutput>>(_paginatedHandler);
        services.AddSingleton<IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(_paginatedHandlerAsync);

        return services.BuildServiceProvider().GetRequiredService<CommandQueryMediator>();
    }

    [Fact]
    public void GivenANullCommand_WhenExecutedSynchronously_ThenArgumentNullExceptionIsThrown()
    {
        var mediator = BuildMediator();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            mediator.ExecuteCommand<EchoCommand, EchoCommandOutput>(null!));

        Assert.Equal("command", exception.ParamName);
        Assert.False(_commandHandler.WasCalled);
    }

    [Fact]
    public async Task GivenANullCommand_WhenExecutedAsynchronously_ThenArgumentNullExceptionIsThrown()
    {
        var mediator = BuildMediator();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            mediator.ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(null!));

        Assert.Equal("command", exception.ParamName);
        Assert.False(_commandHandlerAsync.WasCalled);
    }

    [Fact]
    public void GivenANullQuery_WhenExecutedSynchronously_ThenArgumentNullExceptionIsThrown()
    {
        var mediator = BuildMediator();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            mediator.ExecuteQuery<SearchQuery, SearchQueryOutput>(null!));

        Assert.Equal("query", exception.ParamName);
        Assert.False(_queryHandler.WasCalled);
    }

    [Fact]
    public async Task GivenANullQuery_WhenExecutedAsynchronously_ThenArgumentNullExceptionIsThrown()
    {
        var mediator = BuildMediator();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            mediator.ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(null!));

        Assert.Equal("query", exception.ParamName);
        Assert.False(_queryHandlerAsync.WasCalled);
    }

    [Fact]
    public void GivenANullPaginatedQuery_WhenExecutedSynchronously_ThenArgumentNullExceptionIsThrown()
    {
        var mediator = BuildMediator();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            mediator.ExecutePaginatedQuery<SearchQuery, SearchQueryOutput>(null!));

        Assert.Equal("query", exception.ParamName);
        Assert.False(_paginatedHandler.WasCalled);
    }

    [Fact]
    public async Task GivenANullPaginatedQuery_WhenExecutedAsynchronously_ThenArgumentNullExceptionIsThrown()
    {
        var mediator = BuildMediator();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            mediator.ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(null!));

        Assert.Equal("query", exception.ParamName);
        Assert.False(_paginatedHandlerAsync.WasCalled);
    }
}
