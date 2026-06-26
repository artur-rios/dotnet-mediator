using ArturRios.Mediator.Command.Interfaces;
using ArturRios.Output;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Command;

/// <summary>
/// Dispatches commands to their registered handlers. For each call a new dependency
/// injection scope is created, the matching handler is resolved from it, and the
/// handler is invoked.
/// </summary>
/// <remarks>
/// Register this mediator and the command handlers in your dependency injection
/// container. Because a fresh scope is created per execution, scoped dependencies of a
/// handler (such as a database context) are isolated to a single command execution.
/// </remarks>
/// <param name="scopeFactory">
/// The factory used to create a dependency injection scope for resolving handlers.
/// </param>
public class CommandMediator(IServiceScopeFactory scopeFactory)
{
    /// <summary>
    /// Resolves the synchronous handler for the given command and executes it.
    /// </summary>
    /// <typeparam name="TCommand">The command type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the data payload produced by the handler.</typeparam>
    /// <param name="command">The command instance to execute.</param>
    /// <returns>The <see cref="DataOutput{T}"/> returned by the resolved handler.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="ICommandHandler{TCommand, TOutput}"/> is registered for the
    /// requested command and output types.
    /// </exception>
    public DataOutput<TOutput?> ExecuteCommand<TCommand, TOutput>(TCommand command)
        where TCommand : BaseCommand
        where TOutput : CommandOutput
    {
        using var scoped = scopeFactory.CreateScope();

        var handler = scoped.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TOutput>>();

        return handler.Handle(command);
    }

    /// <summary>
    /// Resolves the asynchronous handler for the given command and executes it.
    /// </summary>
    /// <typeparam name="TCommand">The command type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the data payload produced by the handler.</typeparam>
    /// <param name="command">The command instance to execute.</param>
    /// <returns>
    /// A task that resolves to the <see cref="DataOutput{T}"/> returned by the resolved handler.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="ICommandHandlerAsync{TCommand, TOutput}"/> is registered for the
    /// requested command and output types.
    /// </exception>
    public async Task<DataOutput<TOutput?>> ExecuteCommandAsync<TCommand, TOutput>(TCommand command)
        where TCommand : BaseCommand
        where TOutput : CommandOutput
    {
        using var scoped = scopeFactory.CreateScope();

        var handler = scoped.ServiceProvider.GetRequiredService<ICommandHandlerAsync<TCommand, TOutput>>();

        return await handler.HandleAsync(command);
    }
}
