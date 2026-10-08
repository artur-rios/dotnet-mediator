# Changelog

All notable changes to `ArturRios.Mediator` are recorded in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to
[Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Every `Execute*` method of `CommandMediator`, `QueryMediator` and `CommandQueryMediator` throws
  `ArgumentNullException` for a null command or query, before a dependency injection scope is created, instead of
  passing `null` on to the handler.

### Fixed

- The asynchronous `Execute*Async` methods dispose their dependency injection scope asynchronously. A handler
  depending on a scoped service that only implements `IAsyncDisposable` made the synchronous dispose throw
  `InvalidOperationException` after the handler had run, so the caller lost its result.

## [2.0.0] - 2026-08-24

See [Upgrading from 1.x to 2.0](#upgrading-from-1x-to-20) for the change handler implementations need.

### Changed

- **Breaking:** `ICommandHandlerAsync.HandleAsync`, `IQueryHandlerAsync.HandleAsync` and
  `IPaginatedQueryHandlerAsync.HandleAsync` take a `CancellationToken`, so every implementation must add the parameter.
- The asynchronous methods of `CommandMediator`, `QueryMediator` and `CommandQueryMediator` take an optional
  `CancellationToken` and forward it to the handler; calling code needs no change. An already-canceled token throws
  `OperationCanceledException` before a dependency injection scope is created or a handler resolved.
- `ArturRios.Output` updated from 3.1.0 to 3.2.0.

### Fixed

- The mediators no longer capture the caller's synchronization context (`ConfigureAwait(false)` on every await), which
  deadlocked callers that block on the returned task.
- The README and docs-site code samples use the real `ArturRios.Output` API; they previously called members that do not
  exist.

### Upgrading from 1.x to 2.0

Every asynchronous entry point — on the mediators and on the handler contracts — now takes a
`CancellationToken`. On the mediators the parameter is optional, so **calling code needs no change**:

```csharp
// still compiles, still means the same thing
await mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(command);

// and now this works
await mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(command, ct);
```

**Handler implementations do need a change.** An interface method with a default value is still a new
signature, so every `ICommandHandlerAsync`, `IQueryHandlerAsync` and `IPaginatedQueryHandlerAsync`
implementation must add the parameter:

```diff
-    public async Task<DataOutput<CreateProductOutput?>> HandleAsync(CreateProductCommand command)
+    public async Task<DataOutput<CreateProductOutput?>> HandleAsync(
+        CreateProductCommand command,
+        CancellationToken cancellationToken = default)
```

The synchronous `ICommandHandler`, `IQueryHandler` and `IPaginatedQueryHandler` contracts are unchanged.

A token that is already canceled makes the mediator throw `OperationCanceledException` **before** it
creates a dependency injection scope or resolves a handler, so a canceled dispatch costs nothing.

## [1.0.3] - 2026-07-23

### Changed

- `ArturRios.Output` updated from 3.0.0 to 3.1.0.

## [1.0.2] - 2026-07-23

### Changed

- `ArturRios.Output` updated from 2.0.1 to 3.0.0.

## [1.0.1] - 2026-07-02

### Changed

- Package metadata: a new description, package tags, the docs site as project URL, and the repository URL corrected to
  `artur-rios/dotnet-mediator`.

## [1.0.0] - 2026-06-26

### Added

- `CommandMediator`, `QueryMediator` and the unified `CommandQueryMediator`, which dispatch each command or query to
  its handler resolved from a fresh dependency injection scope.
- Synchronous and asynchronous handler contracts for commands, queries and paginated queries, with `BaseCommand`,
  `BaseQuery`, `CommandOutput` and `QueryOutput` base types.

[Unreleased]: https://github.com/artur-rios/dotnet-mediator/compare/2.0.0...HEAD
[2.0.0]: https://github.com/artur-rios/dotnet-mediator/compare/v1.0.3...2.0.0
[1.0.3]: https://github.com/artur-rios/dotnet-mediator/compare/v1.0.2...v1.0.3
[1.0.2]: https://github.com/artur-rios/dotnet-mediator/compare/v1.0.1...v1.0.2
[1.0.1]: https://github.com/artur-rios/dotnet-mediator/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/artur-rios/dotnet-mediator/releases/tag/v1.0.0
