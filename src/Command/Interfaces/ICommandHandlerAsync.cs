using ArturRios.Output;

namespace ArturRios.Mediator.Command.Interfaces;

/// <summary>
/// Handles a command asynchronously, producing a typed output.
/// </summary>
/// <remarks>
/// Implement this interface for each command that is executed asynchronously and
/// register the implementation in the dependency injection container so the
/// <see cref="CommandMediator"/> can resolve it. Register one handler per
/// command/output pair.
/// </remarks>
/// <typeparam name="TCommand">The command type handled by this handler.</typeparam>
/// <typeparam name="TOutput">The type of the data payload produced by this handler.</typeparam>
public interface ICommandHandlerAsync<in TCommand, TOutput> where TCommand : Mediator.Command.BaseCommand where TOutput : CommandOutput
{
    /// <summary>
    /// Executes the command asynchronously and returns its result.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <returns>
    /// A task that resolves to a <see cref="DataOutput{T}"/> carrying the result payload
    /// along with success state, messages and any errors produced while handling the command.
    /// </returns>
    Task<DataOutput<TOutput?>> HandleAsync(TCommand command);
}
