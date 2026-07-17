+++
title = "Dotnet Mediator"
+++

# Documentation

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
    public async Task<DataOutput<CreateProductOutput?>> HandleAsync(CreateProductCommand command)
    {
        var id = await _repository.InsertAsync(command.Name, command.Price);
        return DataOutput<CreateProductOutput?>.Success(new CreateProductOutput { Id = id });
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
        var result = await mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Errors);
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

- [Command Architecture](https://artur-rios.github.io/dotnet-mediator/command-architecture/)
- [Query Architecture](https://artur-rios.github.io/dotnet-mediator/query-architecture/)

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
