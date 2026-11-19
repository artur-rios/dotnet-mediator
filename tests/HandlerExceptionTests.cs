using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests;

/// <summary>
/// An exception a handler throws reaches the caller as it was thrown, neither wrapped nor swallowed, and the
/// dispatch scope is disposed all the same.
/// </summary>
[Trait("Category", "Unit")]
public class HandlerExceptionTests
{
    private sealed class HandlerFailure() : Exception("the handler failed");

    private static ProbeBehaviour ThrowingBehaviour(HandlerFailure failure) => new()
    {
        OnHandle = _ => throw failure,
        OnHandleAsync = async (_, _) =>
        {
            // After the first await, so the failure comes out of the handler's task, not out of the call.
            await Task.Yield();

            throw failure;
        }
    };

    [Theory]
    [MemberData(nameof(Dispatch.All), MemberType = typeof(Dispatch))]
    public async Task GivenAHandlerThatThrows_WhenDispatched_ThenTheSameExceptionReachesTheCaller(DispatchKind kind)
    {
        var failure = new HandlerFailure();
        await using var provider = ThrowingBehaviour(failure).BuildProvider();

        var thrown = await Assert.ThrowsAsync<HandlerFailure>(() =>
            Dispatch.Run(provider.GetRequiredService<CommandQueryMediator>(), kind, "x",
                TestContext.Current.CancellationToken));

        Assert.Same(failure, thrown);
    }

    [Theory]
    [MemberData(nameof(Dispatch.All), MemberType = typeof(Dispatch))]
    public async Task GivenAHandlerThatThrows_WhenDispatched_ThenTheScopeIsStillDisposed(DispatchKind kind)
    {
        var behaviour = ThrowingBehaviour(new HandlerFailure());
        await using var provider = behaviour.BuildProvider();

        await Assert.ThrowsAsync<HandlerFailure>(() =>
            Dispatch.Run(provider.GetRequiredService<CommandQueryMediator>(), kind, "x",
                TestContext.Current.CancellationToken));

        Assert.True(Assert.Single(behaviour.Created).IsDisposed);
    }

    [Theory]
    [MemberData(nameof(Dispatch.All), MemberType = typeof(Dispatch))]
    public async Task GivenAHandlerThatFailedOnce_WhenDispatchedAgain_ThenTheNextDispatchSucceeds(DispatchKind kind)
    {
        var failure = new HandlerFailure();
        var calls = 0;
        var behaviour = new ProbeBehaviour
        {
            OnHandle = _ =>
            {
                if (calls++ == 0)
                {
                    throw failure;
                }
            },
            OnHandleAsync = (_, _) => calls++ == 0 ? Task.FromException(failure) : Task.CompletedTask
        };
        await using var provider = behaviour.BuildProvider();
        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        await Assert.ThrowsAsync<HandlerFailure>(() =>
            Dispatch.Run(mediator, kind, "first", TestContext.Current.CancellationToken));

        var echoed = await Dispatch.Run(mediator, kind, "second", TestContext.Current.CancellationToken);

        Assert.Equal("second", echoed);
        Assert.Equal(2, behaviour.Created.Count);
        Assert.All(behaviour.Created, resource => Assert.True(resource.IsDisposed));
    }
}
