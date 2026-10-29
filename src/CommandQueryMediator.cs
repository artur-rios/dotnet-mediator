using ArturRios.Mediator.Command;
using ArturRios.Mediator.Command.Interfaces;
using ArturRios.Mediator.Query;
using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Output;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator;

/// <summary>
/// A unified entry point that combines the command and query mediators behind a single
/// type, enabling a CQRS-style separation of writes (commands) and reads (queries) while
/// only depending on one mediator.
/// </summary>
/// <remarks>
/// Register this type in your dependency injection container together with your command
/// and query handlers, then inject it wherever both commands and queries need to be
/// dispatched. Internally it delegates to a <see cref="CommandMediator"/> and a
/// <see cref="QueryMediator"/>, both built from the supplied scope factory.
/// </remarks>
/// <param name="scopeFactory">
/// The factory used to create dependency injection scopes for resolving handlers.
/// </param>
public class CommandQueryMediator(IServiceScopeFactory scopeFactory)
{
    private readonly CommandMediator _commandMediator = new(scopeFactory);
    private readonly QueryMediator _queryMediator = new(scopeFactory);

    /// <summary>
    /// Executes a command synchronously by delegating to the underlying <see cref="CommandMediator"/>.
    /// </summary>
    /// <typeparam name="TCommand">The command type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the data payload produced by the handler.</typeparam>
    /// <param name="command">The command instance to execute.</param>
    /// <returns>The <see cref="DataOutput{T}"/> returned by the resolved handler.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="ICommandHandler{TCommand, TOutput}"/> is registered for the requested types.</exception>
    public DataOutput<TOutput?> ExecuteCommand<TCommand, TOutput>(TCommand command)
        where TCommand : BaseCommand
        where TOutput : CommandOutput
    {
        return _commandMediator.ExecuteCommand<TCommand, TOutput>(command);
    }

    /// <summary>
    /// Executes a command asynchronously by delegating to the underlying <see cref="CommandMediator"/>.
    /// </summary>
    /// <typeparam name="TCommand">The command type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the data payload produced by the handler.</typeparam>
    /// <param name="command">The command instance to execute.</param>
    /// <param name="cancellationToken">Cancels the execution, and the dispatch itself.</param>
    /// <returns>
    /// A task that resolves to the <see cref="DataOutput{T}"/> returned by the resolved handler.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="ICommandHandlerAsync{TCommand, TOutput}"/> is registered for the requested types.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was already canceled.</exception>
    public async Task<DataOutput<TOutput?>> ExecuteCommandAsync<TCommand, TOutput>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : BaseCommand
        where TOutput : CommandOutput
    {
        return await _commandMediator
            .ExecuteCommandAsync<TCommand, TOutput>(command, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a paginated query synchronously by delegating to the underlying <see cref="QueryMediator"/>.
    /// </summary>
    /// <typeparam name="TQuery">The query type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the items in the paginated result.</typeparam>
    /// <param name="query">The query instance to execute, including the requested page metadata.</param>
    /// <returns>The <see cref="PaginatedOutput{T}"/> returned by the resolved handler.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="IPaginatedQueryHandler{TQuery, TOutput}"/> is registered for the requested types.</exception>
    public PaginatedOutput<TOutput> ExecutePaginatedQuery<TQuery, TOutput>(TQuery query)
        where TQuery : BaseQuery
        where TOutput : QueryOutput
    {
        return _queryMediator.ExecutePaginatedQuery<TQuery, TOutput>(query);
    }

    /// <summary>
    /// Executes a paginated query asynchronously by delegating to the underlying <see cref="QueryMediator"/>.
    /// </summary>
    /// <typeparam name="TQuery">The query type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the items in the paginated result.</typeparam>
    /// <param name="query">The query instance to execute, including the requested page metadata.</param>
    /// <param name="cancellationToken">Cancels the execution, and the dispatch itself.</param>
    /// <returns>
    /// A task that resolves to the <see cref="PaginatedOutput{T}"/> returned by the resolved handler.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="IPaginatedQueryHandlerAsync{TQuery, TOutput}"/> is registered for the requested types.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was already canceled.</exception>
    public async Task<PaginatedOutput<TOutput>> ExecutePaginatedQueryAsync<TQuery, TOutput>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : BaseQuery
        where TOutput : QueryOutput
    {
        return await _queryMediator
            .ExecutePaginatedQueryAsync<TQuery, TOutput>(query, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Executes a single-result query synchronously by delegating to the underlying <see cref="QueryMediator"/>.
    /// </summary>
    /// <typeparam name="TQuery">The query type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the data payload produced by the handler.</typeparam>
    /// <param name="query">The query instance to execute.</param>
    /// <returns>The <see cref="DataOutput{T}"/> returned by the resolved handler.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="IQueryHandler{TQuery, TOutput}"/> is registered for the requested types.</exception>
    public DataOutput<TOutput?> ExecuteQuery<TQuery, TOutput>(TQuery query)
        where TQuery : BaseQuery
        where TOutput : QueryOutput
    {
        return _queryMediator.ExecuteQuery<TQuery, TOutput>(query);
    }

    /// <summary>
    /// Executes a single-result query asynchronously by delegating to the underlying <see cref="QueryMediator"/>.
    /// </summary>
    /// <typeparam name="TQuery">The query type to execute.</typeparam>
    /// <typeparam name="TOutput">The type of the data payload produced by the handler.</typeparam>
    /// <param name="query">The query instance to execute.</param>
    /// <param name="cancellationToken">Cancels the execution, and the dispatch itself.</param>
    /// <returns>
    /// A task that resolves to the <see cref="DataOutput{T}"/> returned by the resolved handler.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="query"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">No <see cref="IQueryHandlerAsync{TQuery, TOutput}"/> is registered for the requested types.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was already canceled.</exception>
    public async Task<DataOutput<TOutput?>> ExecuteQueryAsync<TQuery, TOutput>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : BaseQuery
        where TOutput : QueryOutput
    {
        return await _queryMediator
            .ExecuteQueryAsync<TQuery, TOutput>(query, cancellationToken)
            .ConfigureAwait(false);
    }
}
