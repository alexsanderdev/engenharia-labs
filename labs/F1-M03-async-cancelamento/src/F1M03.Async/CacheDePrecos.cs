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
        // TODO: acerto -> ValueTask.FromResult(preco) (caminho síncrono, sem alocar Task).
        //       falha  -> new ValueTask<decimal>(um método async privado que chama _carregar e guarda em _cache).
        _ = (_carregar, _cache);
        throw new NotImplementedException("TODO: implemente ObterPrecoAsync com caminho síncrono no acerto do cache");
    }
}
