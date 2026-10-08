using System.Collections.Concurrent;

namespace F1M07.Api.Products;

/// <summary>Porta de persistência de produtos. Na Fase 3 ganha uma implementação com EF Core.</summary>
public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
    /// <summary>Substitui o produto. Retorna false se o id não existir.</summary>
    Task<bool> UpdateAsync(Product product, CancellationToken ct);
    /// <summary>Remove o produto. Retorna false se o id não existir.</summary>
    Task<bool> RemoveAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Repositório em memória, thread-safe (a API atende requisições em paralelo).
/// Já vem pronto: o foco do lab é a camada HTTP.
/// </summary>
public sealed class InMemoryProductRepository : IProductRepository
{
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct)
    {
        IReadOnlyList<Product> list = [.. _products.Values.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)];
        return Task.FromResult(list);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_products.TryGetValue(id, out var p) ? p : null);

    public Task AddAsync(Product product, CancellationToken ct)
    {
        if (!_products.TryAdd(product.Id, product))
            throw new InvalidOperationException($"Produto {product.Id} já existe.");
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Product product, CancellationToken ct)
    {
        if (!_products.TryGetValue(product.Id, out var current))
            return Task.FromResult(false);
        return Task.FromResult(_products.TryUpdate(product.Id, product, current));
    }

    public Task<bool> RemoveAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_products.TryRemove(id, out _));
}
