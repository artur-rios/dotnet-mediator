using ArturRios.Mediator.Command;
using ArturRios.Mediator.Command.Interfaces;
using ArturRios.Mediator.Query;
using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Mediator.Tests.Command;
using ArturRios.Mediator.Tests.Query;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests;

/// <summary>
/// The token reaches the handler, and a token that is already canceled stops the dispatch before a
/// dependency injection scope is created.
/// </summary>
[Trait("Category", "Unit")]
public class CancellationTests
{
    private static ServiceProvider BuildProvider(params Action<IServiceCollection>[] register)
    {
        var services = new ServiceCollection();

        services.AddSingleton<CommandMediator>();
        services.AddSingleton<QueryMediator>();
        services.AddSingleton<CommandQueryMediator>();

        foreach (var registration in register)
        {
            registration(services);
        }

        return services.BuildServiceProvider();
    }

    private sealed class CountingScopeFactory(IServiceScopeFactory inner) : IServiceScopeFactory
    {
        public int ScopesCreated { get; private set; }

        public IServiceScope CreateScope()
        {
            ScopesCreated++;

            return inner.CreateScope();
        }
    }

    [Fact]
    public async Task GivenAToken_WhenACommandIsDispatched_ThenTheHandlerReceivesIt()
    {
        var handler = new EchoCommandHandlerAsync();

        await using var provider = BuildProvider(services =>
            services.AddSingleton<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>>(handler));

        using var cancellation = new CancellationTokenSource();

        await provider.GetRequiredService<CommandQueryMediator>()
            .ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" }, cancellation.Token);

        Assert.Equal(cancellation.Token, handler.ReceivedToken);
    }

    [Fact]
    public async Task GivenAToken_WhenAQueryIsDispatched_ThenTheHandlerReceivesIt()
    {
        var handler = new SearchQueryHandlerAsync();

        await using var provider = BuildProvider(services =>
            services.AddSingleton<IQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(handler));

        using var cancellation = new CancellationTokenSource();

        await provider.GetRequiredService<CommandQueryMediator>()
            .ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "x" }, cancellation.Token);

        Assert.Equal(cancellation.Token, handler.ReceivedToken);
    }

    [Fact]
    public async Task GivenAToken_WhenAPaginatedQueryIsDispatched_ThenTheHandlerReceivesIt()
    {
        var handler = new SearchPaginatedQueryHandlerAsync();

        await using var provider = BuildProvider(services =>
            services.AddSingleton<IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(handler));

        using var cancellation = new CancellationTokenSource();

        await provider.GetRequiredService<CommandQueryMediator>()
            .ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(
                new SearchQuery { Term = "x" }, cancellation.Token);

        Assert.Equal(cancellation.Token, handler.ReceivedToken);
    }

    [Fact]
    public async Task GivenACancelledToken_WhenACommandIsDispatched_ThenCancellationIsThrown()
    {
        var handler = new EchoCommandHandlerAsync();

        await using var provider = BuildProvider(services =>
            services.AddSingleton<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>>(handler));

        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<CommandQueryMediator>()
                .ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" }, cancellation.Token));

        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GivenACancelledToken_WhenAQueryIsDispatched_ThenCancellationIsThrown()
    {
        var handler = new SearchQueryHandlerAsync();

        await using var provider = BuildProvider(services =>
            services.AddSingleton<IQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(handler));

        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<CommandQueryMediator>()
                .ExecuteQueryAsync<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "x" }, cancellation.Token));

        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GivenACancelledToken_WhenAPaginatedQueryIsDispatched_ThenCancellationIsThrown()
    {
        var handler = new SearchPaginatedQueryHandlerAsync();

        await using var provider = BuildProvider(services =>
            services.AddSingleton<IPaginatedQueryHandlerAsync<SearchQuery, SearchQueryOutput>>(handler));

        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.GetRequiredService<CommandQueryMediator>()
                .ExecutePaginatedQueryAsync<SearchQuery, SearchQueryOutput>(new SearchQuery { Term = "x" }, cancellation.Token));

        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GivenACancelledToken_WhenACommandIsDispatched_ThenNoScopeIsCreated()
    {
        await using var provider = BuildProvider(services =>
            services.AddSingleton<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>>(new EchoCommandHandlerAsync()));

        var counting = new CountingScopeFactory(provider.GetRequiredService<IServiceScopeFactory>());
        var mediator = new CommandMediator(counting);

        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mediator.ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" }, cancellation.Token));

        Assert.Equal(0, counting.ScopesCreated);
    }

    [Fact]
    public async Task GivenNoToken_WhenACommandIsDispatched_ThenTheHandlerReceivesNone()
    {
        var handler = new EchoCommandHandlerAsync();

        await using var provider = BuildProvider(services =>
            services.AddSingleton<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>>(handler));

        await provider.GetRequiredService<CommandQueryMediator>()
            .ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" });

        Assert.Equal(CancellationToken.None, handler.ReceivedToken);
        Assert.True(handler.WasCalled);
    }
}
