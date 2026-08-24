using ArturRios.Mediator.Command;
using ArturRios.Mediator.Command.Interfaces;
using ArturRios.Mediator.Query;
using ArturRios.Mediator.Query.Interfaces;
using ArturRios.Output;
using Microsoft.Extensions.DependencyInjection;

namespace ArturRios.Mediator.Tests.Functional;

/// <summary>
/// The commands, queries, handlers and dispatch calls documented in the README and on the docs site,
/// written out verbatim and executed through a real service provider. Because it compiles, a sample that
/// drifts away from the API — as every one of them had — stops the build instead of misleading a reader.
/// </summary>
[Trait("Category", "Functional")]
public class DocumentedUsageTests
{
    private sealed record Product(Guid Id, string Name, decimal Price);

    private interface IProductRepository
    {
        Task<Guid> InsertAsync(string name, decimal price);

        Task<Product?> FindByIdAsync(Guid id);

        Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(string? nameFilter, int pageNumber, int pageSize);
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products = [];

        public Task<Guid> InsertAsync(string name, decimal price)
        {
            var product = new Product(Guid.NewGuid(), name, price);

            _products.Add(product);

            return Task.FromResult(product.Id);
        }

        public Task<Product?> FindByIdAsync(Guid id) =>
            Task.FromResult(_products.FirstOrDefault(product => product.Id == id));

        public Task<(IReadOnlyList<Product> Items, int Total)> ListAsync(string? nameFilter, int pageNumber, int pageSize)
        {
            var matching = _products
                .Where(product => nameFilter is null || product.Name.Contains(nameFilter, StringComparison.Ordinal))
                .ToList();

            IReadOnlyList<Product> page = matching
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult((page, matching.Count));
        }
    }

    // --- The documented command ------------------------------------------------------------------

    private sealed class CreateProductCommand : BaseCommand
    {
        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }

    private sealed class CreateProductOutput : CommandOutput
    {
        public Guid Id { get; set; }
    }

    private sealed class CreateProductHandler(IProductRepository repository)
        : ICommandHandlerAsync<CreateProductCommand, CreateProductOutput>
    {
        public async Task<DataOutput<CreateProductOutput?>> HandleAsync(
            CreateProductCommand command,
            CancellationToken cancellationToken = default)
        {
            var id = await repository.InsertAsync(command.Name, command.Price);

            return DataOutput<CreateProductOutput?>.New.WithData(new CreateProductOutput { Id = id });
        }
    }

    // --- The documented single-result query ------------------------------------------------------

    private sealed class GetProductQuery : BaseQuery
    {
        public Guid Id { get; set; }
    }

    private sealed class GetProductOutput : QueryOutput
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }

    private sealed class GetProductHandler(IProductRepository repository)
        : IQueryHandlerAsync<GetProductQuery, GetProductOutput>
    {
        public async Task<DataOutput<GetProductOutput?>> HandleAsync(
            GetProductQuery query,
            CancellationToken cancellationToken = default)
        {
            var product = await repository.FindByIdAsync(query.Id);

            return product is null
                ? DataOutput<GetProductOutput?>.New.WithError("Product not found.")
                : DataOutput<GetProductOutput?>.New.WithData(new GetProductOutput
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price
                });
        }
    }

    // --- The documented paginated query ----------------------------------------------------------

    private sealed class ListProductsQuery : BaseQuery
    {
        public string? NameFilter { get; set; }
    }

    private sealed class ProductListItem : QueryOutput
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }
    }

    private sealed class ListProductsHandler(IProductRepository repository)
        : IPaginatedQueryHandlerAsync<ListProductsQuery, ProductListItem>
    {
        public async Task<PaginatedOutput<ProductListItem>> HandleAsync(
            ListProductsQuery query,
            CancellationToken cancellationToken = default)
        {
            var (items, total) = await repository.ListAsync(query.NameFilter, query.PageNumber, query.PageSize);

            return PaginatedOutput<ProductListItem>.New
                .WithData(items.Select(p => new ProductListItem { Id = p.Id, Name = p.Name, Price = p.Price }).ToList())
                .WithPagination(query.PageNumber, query.PageSize, total);
        }
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<CommandMediator>();
        services.AddSingleton<QueryMediator>();
        services.AddSingleton<CommandQueryMediator>();

        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        services.AddScoped<ICommandHandlerAsync<CreateProductCommand, CreateProductOutput>, CreateProductHandler>();
        services.AddScoped<IQueryHandlerAsync<GetProductQuery, GetProductOutput>, GetProductHandler>();
        services.AddScoped<IPaginatedQueryHandlerAsync<ListProductsQuery, ProductListItem>, ListProductsHandler>();

        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GivenTheDocumentedRegistration_WhenACommandIsDispatched_ThenItsOutputComesBackSuccessful()
    {
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        var result = await mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(
            new CreateProductCommand { Name = "Widget", Price = 9.99m });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEqual(Guid.Empty, result.Data!.Id);
    }

    [Fact]
    public async Task GivenAProductThatWasCreated_WhenQueriedById_ThenItComesBack()
    {
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        var created = await mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(
            new CreateProductCommand { Name = "Widget", Price = 9.99m });

        var found = await mediator.ExecuteQueryAsync<GetProductQuery, GetProductOutput>(
            new GetProductQuery { Id = created.Data!.Id });

        Assert.True(found.Success);
        Assert.Equal("Widget", found.Data!.Name);
        Assert.Equal(9.99m, found.Data.Price);
    }

    [Fact]
    public async Task GivenAnIdThatDoesNotExist_WhenQueried_ThenTheEnvelopeCarriesTheError()
    {
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        var found = await mediator.ExecuteQueryAsync<GetProductQuery, GetProductOutput>(
            new GetProductQuery { Id = Guid.NewGuid() });

        Assert.False(found.Success);
        Assert.Contains("Product not found.", found.Errors);
        Assert.Null(found.Data);
    }

    [Fact]
    public async Task GivenSeveralProducts_WhenListedPageByPage_ThenThePaginationMetadataIsCorrect()
    {
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        foreach (var index in Enumerable.Range(1, 25))
        {
            await mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(
                new CreateProductCommand { Name = $"Widget {index:D2}", Price = index });
        }

        var page = await mediator.ExecutePaginatedQueryAsync<ListProductsQuery, ProductListItem>(
            new ListProductsQuery { NameFilter = "Widget", PageNumber = 2, PageSize = 20 });

        Assert.True(page.Success);
        Assert.NotNull(page.Data);
        Assert.Equal(5, page.Data!.Count);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(20, page.PageSize);
        Assert.Equal(25, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task GivenAFilterThatMatchesNothing_WhenListed_ThenAnEmptySuccessfulPageComesBack()
    {
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        var page = await mediator.ExecutePaginatedQueryAsync<ListProductsQuery, ProductListItem>(
            new ListProductsQuery { NameFilter = "nothing", PageNumber = 1, PageSize = 20 });

        Assert.True(page.Success);
        Assert.NotNull(page.Data);
        Assert.Empty(page.Data!);
        Assert.Equal(0, page.TotalItems);
        Assert.Equal(0, page.TotalPages);
    }

    [Fact]
    public async Task GivenAQueryWithNoExplicitPage_WhenListed_ThenTheDocumentedDefaultsApply()
    {
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        await mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(
            new CreateProductCommand { Name = "Widget", Price = 1m });

        var page = await mediator.ExecutePaginatedQueryAsync<ListProductsQuery, ProductListItem>(
            new ListProductsQuery());

        Assert.Equal(1, page.PageNumber);
        Assert.Equal(100, page.PageSize);
    }

    [Fact]
    public async Task GivenNoHandlerRegistered_WhenDispatching_ThenTheFailureNamesTheMissingHandler()
    {
        var services = new ServiceCollection();

        services.AddSingleton<CommandQueryMediator>();

        await using var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<CommandQueryMediator>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(new CreateProductCommand()));

        Assert.Contains(nameof(ICommandHandlerAsync<CreateProductCommand, CreateProductOutput>), exception.Message);
    }

    [Fact]
    public async Task GivenTheSeparateMediators_WhenResolved_ThenTheyDispatchTheSameWayTheFacadeDoes()
    {
        await using var provider = BuildProvider();

        var commands = provider.GetRequiredService<CommandMediator>();
        var queries = provider.GetRequiredService<QueryMediator>();

        var created = await commands.ExecuteCommandAsync<CreateProductCommand, CreateProductOutput>(
            new CreateProductCommand { Name = "Widget", Price = 5m });

        var found = await queries.ExecuteQueryAsync<GetProductQuery, GetProductOutput>(
            new GetProductQuery { Id = created.Data!.Id });

        Assert.True(found.Success);
        Assert.Equal("Widget", found.Data!.Name);
    }
}
