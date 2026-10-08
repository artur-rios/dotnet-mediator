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

## Upgrading

Releases that need changes in consuming code carry an upgrade guide in the changelog:

- From 1.x to 2.0: [Upgrading from 1.x to 2.0]({{< relref "changelog#upgrading-from-1x-to-20" >}})

## Legal Details

This project is licensed under the [MIT License](https://en.wikipedia.org/wiki/MIT_License). A copy of the license is available at [LICENSE](https://github.com/artur-rios/dotnet-mediator/blob/main/LICENSE) in the repository.
