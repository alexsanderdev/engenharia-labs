using System.Runtime.CompilerServices;

namespace F1M03.Async;

/// <summary>
/// Lê uma fonte paginada (ex.: API de pedidos de um parceiro) como um fluxo <see cref="IAsyncEnumerable{T}"/>:
/// o consumidor faz <c>await foreach</c> e as páginas são buscadas sob demanda.
/// </summary>
public static class LeitorPaginado
{
    /// <summary>
    /// Busca as páginas 1, 2, 3... e entrega item a item. Para quando uma página vier VAZIA.
    /// <para>Páginas só são buscadas quando o consumidor pede mais itens (se ele parar no meio,
    /// nenhuma página extra é buscada).</para>
    /// <para>O token (inclusive o passado via <c>WithCancellation</c>) é repassado para
    /// <paramref name="buscarPagina"/>.</para>
    /// </summary>
    public static async IAsyncEnumerable<T> LerTodosAsync<T>(
        Func<int, CancellationToken, Task<IReadOnlyList<T>>> buscarPagina,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: laço pagina = 1, 2, 3...: await buscarPagina(pagina, cancellationToken);
        //       página vazia -> yield break; senão, yield return de cada item.
        await Task.CompletedTask.ConfigureAwait(false);
        if (buscarPagina is not null)
            throw new NotImplementedException("TODO: implemente LerTodosAsync com yield return");
        yield break;
    }
}
