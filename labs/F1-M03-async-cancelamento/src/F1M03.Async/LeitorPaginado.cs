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
        ArgumentNullException.ThrowIfNull(buscarPagina);

        for (var pagina = 1; ; pagina++)
        {
            var itens = await buscarPagina(pagina, cancellationToken).ConfigureAwait(false);
            if (itens.Count == 0)
                yield break;

            foreach (var item in itens)
                yield return item;
        }
    }
}
