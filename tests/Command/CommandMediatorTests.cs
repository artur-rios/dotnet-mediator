using ArturRios.Mediator.Command;
using ArturRios.Mediator.Command.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests.Command;

public class CommandMediatorTests
{
    private static CommandMediator BuildMediator(IServiceProvider provider) =>
        new(provider.GetRequiredService<IServiceScopeFactory>());

    [Fact]
    public void GivenRegisteredHandler_WhenExecutingCommand_ThenResolvesAndInvokesHandler()
    {
        var handler = new EchoCommandHandler();
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<EchoCommand, EchoCommandOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var command = new EchoCommand { Value = "ping" };

        var result = mediator.ExecuteCommand<EchoCommand, EchoCommandOutput>(command);

        Assert.True(handler.WasCalled);
        Assert.Same(command, handler.ReceivedCommand);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("ping", result.Data!.Value);
    }

    [Fact]
    public async Task GivenRegisteredAsyncHandler_WhenExecutingCommandAsync_ThenResolvesAndInvokesHandler()
    {
        var handler = new EchoCommandHandlerAsync();
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandlerAsync<EchoCommand, EchoCommandOutput>>(handler);
        var mediator = BuildMediator(services.BuildServiceProvider());

        var command = new EchoCommand { Value = "pong" };

        var result = await mediator.ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(command);

        Assert.True(handler.WasCalled);
        Assert.Same(command, handler.ReceivedCommand);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("pong", result.Data!.Value);
    }

    [Fact]
    public void GivenHandlerThatFails_WhenExecutingCommand_ThenReturnsHandlerOutput()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICommandHandler<EchoCommand, EchoCommandOutput>, FailingCommandHandler>();
        var mediator = BuildMediator(services.BuildServiceProvider());

        var result = mediator.ExecuteCommand<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" });

        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Contains(FailingCommandHandler.ErrorMessage, result.Errors);
    }

    [Fact]
    public void GivenNoRegisteredHandler_WhenExecutingCommand_ThenThrows()
    {
        var mediator = BuildMediator(new ServiceCollection().BuildServiceProvider());

        Assert.Throws<InvalidOperationException>(() =>
            mediator.ExecuteCommand<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" }));
    }

    [Fact]
    public async Task GivenNoRegisteredHandler_WhenExecutingCommandAsync_ThenThrows()
    {
        var mediator = BuildMediator(new ServiceCollection().BuildServiceProvider());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.ExecuteCommandAsync<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "x" }));
    }

    [Fact]
    public void GivenScopedHandler_WhenExecutingCommandMultipleTimes_ThenResolvesFromNewScopeEachCall()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICommandHandler<EchoCommand, EchoCommandOutput>, EchoCommandHandler>();
        var mediator = BuildMediator(services.BuildServiceProvider());

        // Each call opens its own scope, so a scoped handler is resolved fresh and
        // state never leaks between executions.
        var first = mediator.ExecuteCommand<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "a" });
        var second = mediator.ExecuteCommand<EchoCommand, EchoCommandOutput>(new EchoCommand { Value = "b" });

        Assert.Equal("a", first.Data!.Value);
        Assert.Equal("b", second.Data!.Value);
    }
}
