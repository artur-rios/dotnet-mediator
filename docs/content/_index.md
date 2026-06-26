+++
title = "Dotnet Mediator"
+++

`ArturRios.Mediator` is a lightweight .NET library that implements the [Mediator pattern](https://refactoring.guru/design-patterns/mediator) on top of the built-in dependency injection container, providing a clean CQRS-style separation between **commands** (write operations) and **queries** (read operations).

Each command or query is dispatched to a dedicated handler resolved from a fresh DI scope, so scoped dependencies such as a database context are isolated per execution. Both synchronous and asynchronous variants are supported for every handler type.

## Installation

```
dotnet add package ArturRios.Mediator
```

The package targets **net10.0** and depends on [`ArturRios.Output`](https://www.nuget.org/packages/ArturRios.Output), which provides the `DataOutput<T>` and `PaginatedOutput<T>` result envelopes.

## Mediator Types

The library ships three mediator classes and a full set of handler interfaces:

| Type | Role |
|---|---|
| `CommandMediator` | Dispatches write operations (commands) |
| `QueryMediator` | Dispatches read operations (queries, paginated queries) |
| `CommandQueryMediator` | Unified facade that delegates to both mediators |

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

### 2. Define a command and dispatch it

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

// In a controller or service:
var result = await mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(command);
```

## Architecture Documentation

- [Command Architecture]({{< ref "command-architecture" >}}) — data flow, class diagram, and sequence diagrams for command dispatch
- [Query Architecture]({{< ref "query-architecture" >}}) — data flow, class diagram, and sequence diagrams for query dispatch (single-result and paginated)
