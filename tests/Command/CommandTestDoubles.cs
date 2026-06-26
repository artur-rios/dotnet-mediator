using ArturRios.Mediator.Command;
using ArturRios.Mediator.Command.Interfaces;
using ArturRios.Output;

namespace ArturRios.Mediator.Tests.Command;

/// <summary>
/// Command carrying a value to be echoed back by its handler.
/// </summary>
public class EchoCommand : BaseCommand
{
    public required string Value { get; init; }
}

/// <summary>
/// Output produced by the echo handlers.
/// </summary>
public class EchoCommandOutput : CommandOutput
{
    public required string Value { get; init; }
}

/// <summary>
/// Synchronous handler that echoes the command value and records its invocation.
/// </summary>
public class EchoCommandHandler : ICommandHandler<EchoCommand, EchoCommandOutput>
{
    public bool WasCalled { get; private set; }
    public EchoCommand? ReceivedCommand { get; private set; }

    public DataOutput<EchoCommandOutput?> Handle(EchoCommand command)
    {
        WasCalled = true;
        ReceivedCommand = command;

        return DataOutput<EchoCommandOutput?>.New.WithData(new EchoCommandOutput { Value = command.Value });
    }
}

/// <summary>
/// Asynchronous handler that echoes the command value and records its invocation.
/// </summary>
public class EchoCommandHandlerAsync : ICommandHandlerAsync<EchoCommand, EchoCommandOutput>
{
    public bool WasCalled { get; private set; }
    public EchoCommand? ReceivedCommand { get; private set; }

    public async Task<DataOutput<EchoCommandOutput?>> HandleAsync(EchoCommand command)
    {
        await Task.Yield();

        WasCalled = true;
        ReceivedCommand = command;

        return DataOutput<EchoCommandOutput?>.New.WithData(new EchoCommandOutput { Value = command.Value });
    }
}

/// <summary>
/// Synchronous handler that returns a failed output with an error message.
/// </summary>
public class FailingCommandHandler : ICommandHandler<EchoCommand, EchoCommandOutput>
{
    public const string ErrorMessage = "command handler failed";

    public DataOutput<EchoCommandOutput?> Handle(EchoCommand command)
    {
        var output = DataOutput<EchoCommandOutput?>.New;
        output.AddError(ErrorMessage);

        return output;
    }
}
