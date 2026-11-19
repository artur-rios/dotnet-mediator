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
            mediator.ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(null!, TestContext.Current.CancellationToken));

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
            mediator.ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(null!, TestContext.Current.CancellationToken));

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
            mediator.ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(
                null!, TestContext.Current.CancellationToken));

        Assert.Equal("query", exception.ParamName);
        Assert.False(_paginatedHandlerAsync.WasCalled);
    }

    [Theory]
    [MemberData(nameof(Dispatch.All), MemberType = typeof(Dispatch))]
    public async Task GivenANullRequest_WhenDispatched_ThenNoScopeIsCreated(DispatchKind kind)
    {
        await using var provider = new ProbeBehaviour().BuildProvider();

        var counting = new CountingScopeFactory(provider.GetRequiredService<IServiceScopeFactory>());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Dispatch.Run(new CommandQueryMediator(counting), kind, null, TestContext.Current.CancellationToken));

        Assert.Equal(0, counting.ScopesCreated);
    }

    [Theory]
    [MemberData(nameof(Dispatch.All), MemberType = typeof(Dispatch))]
    public async Task GivenANullRequestAndNoHandler_WhenDispatched_ThenTheNullIsReportedRatherThanTheMissingHandler(
        DispatchKind kind)
    {
        await using var provider = new ServiceCollection().BuildServiceProvider();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Dispatch.Run(new CommandQueryMediator(provider.GetRequiredService<IServiceScopeFactory>()), kind, null,
                TestContext.Current.CancellationToken));
    }

    [Theory]
    [MemberData(nameof(Dispatch.Asynchronous), MemberType = typeof(Dispatch))]
    public async Task GivenANullRequestAndACancelledToken_WhenDispatched_ThenTheNullIsReportedFirst(DispatchKind kind)
    {
        await using var provider = new ProbeBehaviour().BuildProvider();

        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Dispatch.Run(provider.GetRequiredService<CommandQueryMediator>(), kind, null, cancellation.Token));
    }
}
