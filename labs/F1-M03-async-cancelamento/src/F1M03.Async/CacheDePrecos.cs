using System.Collections.Concurrent;

namespace F1M03.Async;

/// <summary>
/// Cache de preços do catálogo. A maioria das chamadas acerta o cache e termina de forma
/// SÍNCRONA: caso de uso clássico para <see cref="ValueTask{TResult}"/> (evita alocar uma Task por chamada).
/// </summary>
/// <param name="carregar">Busca o preço na fonte (banco/API) quando não está em cache.</param>
public sealed class CacheDePrecos(Func<Guid, CancellationToken, Task<decimal>> carregar)
{
    private readonly Func<Guid, CancellationToken, Task<decimal>> _carregar =
        carregar ?? throw new ArgumentNullException(nameof(carregar));
    private readonly ConcurrentDictionary<Guid, decimal> _cache = new();

    /// <summary>
    /// Retorna o preço. Acerto no cache: <see cref="ValueTask{TResult}"/> já completada, sem chamar a fonte.
    /// Falha no cache: chama a fonte REPASSANDO o <paramref name="cancellationToken"/>, guarda e retorna.
    /// Se a fonte falhar ou for cancelada, nada é guardado.
    /// </summary>
    public ValueTask<decimal> ObterPrecoAsync(Guid produtoId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(produtoId, out var preco))
            return ValueTask.FromResult(preco);

        return new ValueTask<decimal>(CarregarEGuardarAsync(produtoId, cancellationToken));
    }

    private async Task<decimal> CarregarEGuardarAsync(Guid produtoId, CancellationToken cancellationToken)
    {
        var preco = await _carregar(produtoId, cancellationToken).ConfigureAwait(false);
        _cache[produtoId] = preco;
        return preco;
    }
}
