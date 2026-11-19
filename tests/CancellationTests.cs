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

    [Theory]
    [MemberData(nameof(Dispatch.Asynchronous), MemberType = typeof(Dispatch))]
    public async Task GivenACancelledToken_WhenDispatched_ThenNoScopeIsCreated(DispatchKind kind)
    {
        await using var provider = new ProbeBehaviour().BuildProvider();

        var counting = new CountingScopeFactory(provider.GetRequiredService<IServiceScopeFactory>());
        var mediator = new CommandQueryMediator(counting);

        using var cancellation = new CancellationTokenSource();

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Dispatch.Run(mediator, kind, "x", cancellation.Token));

        Assert.Equal(0, counting.ScopesCreated);
    }

    [Theory]
    [MemberData(nameof(Dispatch.Asynchronous), MemberType = typeof(Dispatch))]
    public async Task GivenATokenCancelledWhileTheHandlerRuns_WhenDispatched_ThenTheCancellationReachesTheCallerAndTheScopeIsDisposed(
        DispatchKind kind)
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var behaviour = new ProbeBehaviour
        {
            // Waits for nothing but the token: the handler only finishes when the dispatch is cancelled.
            OnHandleAsync = async (_, cancellationToken) =>
            {
                started.SetResult();

                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        };
        await using var provider = behaviour.BuildProvider();

        using var cancellation = new CancellationTokenSource();

        var dispatch = Dispatch.Run(provider.GetRequiredService<CommandQueryMediator>(), kind, "x", cancellation.Token);

        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await cancellation.CancelAsync();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatch);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.True(Assert.Single(behaviour.Created).DisposedAsynchronously);
    }

    [Fact]
    public async Task GivenNoToken_WhenACommandIsDispatched_ThenTheHandlerReceivesNone()
    {
        var handler = new EchoCommandHandlerAsync();

        await using var provider = BuildProvider(services =>
            services.AddSingleton<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>>(handler));

        // The point of the test is the call without a token, so the test's own token is not passed.
#pragma warning disable xUnit1051
        await provider.GetRequiredService<CommandQueryMediator>()
            .ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" });
#pragma warning restore xUnit1051

        Assert.Equal(CancellationToken.None, handler.ReceivedToken);
        Assert.True(handler.WasCalled);
    }
}
