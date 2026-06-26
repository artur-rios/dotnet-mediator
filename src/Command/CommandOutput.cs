namespace ArturRios.Mediator.Command;

/// <summary>
/// Base type for the data payload returned by a command handler. The payload is
/// wrapped in a <see cref="ArturRios.Output.DataOutput{T}"/> so callers also receive
/// success state, messages and errors alongside the data.
/// </summary>
/// <remarks>
/// Derive from this type to describe the result of executing a command (for example,
/// the identifier of a newly created entity).
/// </remarks>
public abstract class CommandOutput;
