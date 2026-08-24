---
title: Documentation
linkTitle: Documentation
weight: 20
description: >-
  `ArturRios.Mediator` is a lightweight .NET library that implements the [Mediator pattern](https://refactoring.guru/design-patterns/mediator) on top of the...
---

`ArturRios.Mediator` is a lightweight .NET library that implements the [Mediator pattern](https://refactoring.guru/design-patterns/mediator) on top of the built-in dependency injection container, providing a clean CQRS-style separation between **commands** (write operations) and **queries** (read operations).

Each command or query is dispatched to a dedicated handler resolved from a fresh DI scope, so scoped dependencies such as a database context are isolated per execution. Both synchronous and asynchronous variants are supported for every handler type.

## Installation

```bash
dotnet add package ArturRios.Mediator
```

The package targets **net10.0** and depends on [`ArturRios.Output`](https://www.nuget.org/packages/ArturRios.Output), which provides the `DataOutput<T>` and `PaginatedOutput<T>` result envelopes.

## Quick Start

### 1. Register the mediators and handlers

```csharp
// Program.cs / Startup.cs
builder.Services.AddSingleton<CommandMediator>();
builder.Services.AddSingleton<QueryMediator>();
// or the combined entry point:
builder.Services.AddSingleton<CommandQueryMediator>();

// Register each handler
builder.Services.AddScoped<ICommandHandler<CreateProductCommand, CreateProductOutput>, CreateProductHandler>();
builder.Services.AddScoped<IQueryHandler<GetProductQuery, GetProductOutput>, GetProductHandler>();
```

### 2. Define a command

```csharp
public class CreateProductCommand : BaseCommand
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class CreateProductOutput : CommandOutput
{
    public Guid Id { get; set; }
}
```

### 3. Implement a handler

```csharp
public class CreateProductHandler : ICommandHandlerAsync<CreateProductCommand, CreateProductOutput>
{
    public async Task<DataOutput<CreateProductOutput?>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var id = await _repository.InsertAsync(command.Name, command.Price, cancellationToken);
        return DataOutput<CreateProductOutput?>.New.WithData(new CreateProductOutput { Id = id });
    }
}
```

### 4. Dispatch

```csharp
public class ProductsController(CommandQueryMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductCommand command)
    {
        var result = await mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(
            command, HttpContext.RequestAborted);

        return result.Success ? Ok(result.Data) : BadRequest(result.Errors);
    }
}
```

## Architecture

The library ships three mediator classes and a full set of handler interfaces:

| Type | Role |
|---|---|
| `CommandMediator` | Dispatches write operations (commands) |
| `QueryMediator` | Dispatches read operations (queries, paginated queries) |
| `CommandQueryMediator` | Unified facade that delegates to both mediators |

For detailed architecture documentation and sequence diagrams see:

- [Command Architecture](https://artur-rios.github.io/dotnet-mediator/docs/command-architecture/)
- [Query Architecture](https://artur-rios.github.io/dotnet-mediator/docs/query-architecture/)

## Upgrading to 2.0

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

## Testing

The test suite is xUnit, and every test is named with the Given / When / Then pattern. Every test class
carries a `Category` trait, so the two kinds can be run — and reported — separately:

```bash
dotnet test src/ArturRios.Mediator.sln --filter "Category=Unit"
dotnet test src/ArturRios.Mediator.sln --filter "Category=Functional"
```

Unit tests exercise the code in isolation against test doubles.
Functional tests execute the commands, queries and handlers the documentation shows, verbatim, through a real service provider - so a sample that drifts from the API stops the build.
CI runs the two as separate jobs, and both must pass before a pull request can be merged.

## Versioning

Semantic Versioning (SemVer). Breaking changes result in a new major version. New methods or non-breaking behavior
changes increment the minor version; fixes or tweaks increment the patch.

## Build, test and publish

Use the official [.NET CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/) to build, test and publish the project and Git for source control.
If you want, optional helper toolsets I built to facilitate these tasks are available:

- [Dotnet Tools](https://github.com/artur-rios/dotnet-tools)
- [Python Dotnet Tools](https://github.com/artur-rios/python-dotnet-tools)

## Legal Details

This project is licensed under the [MIT License](https://en.wikipedia.org/wiki/MIT_License). A copy of the license is available at [LICENSE](https://github.com/artur-rios/dotnet-mediator/blob/main/LICENSE) in the repository.
