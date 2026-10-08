using System.Linq.Expressions;

namespace F1M04.Linq;

/// <summary>
/// Consultas em memória sobre o catálogo de produtos.
/// </summary>
public static class ConsultasCatalogo
{
    /// <summary>
    /// Produtos ativos de uma categoria (comparação sem diferenciar maiúsculas/minúsculas),
    /// ordenados por nome.
    /// </summary>
    public static IReadOnlyList<Produto> AtivosDaCategoria(IEnumerable<Produto> produtos, string categoria)
    {
        throw new NotImplementedException("TODO: Where (Ativo + categoria com OrdinalIgnoreCase) + OrderBy(Nome) + ToList");
    }

    /// <summary>
    /// Busca paginada no catálogo.
    /// Regras: filtra por termo no nome (ignora maiúsculas/minúsculas), faixa de preço e ativos;
    /// ordena por preço e, em empate, por nome; página começa em 1.
    /// Lança <see cref="ArgumentOutOfRangeException"/> se Pagina &lt; 1 ou TamanhoPagina fora de 1..100.
    /// A fonte <paramref name="produtos"/> deve ser enumerada UMA única vez.
    /// </summary>
    public static Pagina<Produto> Buscar(IEnumerable<Produto> produtos, FiltroCatalogo filtro)
    {
        throw new NotImplementedException("TODO: valide a paginação, componha os Where opcionais, ordene, materialize UMA vez e só então conte e pagine (Skip/Take)");
    }

    /// <summary>
    /// Quantidade de produtos por categoria (use <c>CountBy</c>, .NET 9+).
    /// </summary>
    public static IReadOnlyDictionary<string, int> ContarPorCategoria(IEnumerable<Produto> produtos)
    {
        throw new NotImplementedException("TODO: produtos.CountBy(...).ToDictionary()");
    }

    /// <summary>
    /// Índice de produtos por categoria (use <c>ToLookup</c>): consultar uma categoria inexistente
    /// deve devolver sequência vazia, não exceção.
    /// </summary>
    public static ILookup<string, Produto> IndicePorCategoria(IEnumerable<Produto> produtos)
    {
        throw new NotImplementedException("TODO: ToLookup por categoria, com comparer que ignora maiúsculas/minúsculas");
    }

    /// <summary>
    /// Divide os SKUs em lotes de tamanho fixo (o último pode ser menor) — use <c>Chunk</c>.
    /// Útil para enviar o catálogo a um serviço externo que aceita no máximo N itens por chamada.
    /// </summary>
    public static IReadOnlyList<string[]> SkusEmLotes(IEnumerable<Produto> produtos, int tamanhoLote)
    {
        throw new NotImplementedException("TODO: Select(Sku) + Chunk(tamanhoLote) + ToList");
    }

    /// <summary>
    /// Predicado de faixa de preço como <see cref="Expression{TDelegate}"/>, para funcionar tanto
    /// com <see cref="IQueryable{T}"/> (um provider como o EF Core traduz para SQL) quanto
    /// em memória (via <c>Compile()</c>).
    /// </summary>
    public static Expression<Func<Produto, bool>> FaixaDePreco(decimal minimo, decimal maximo)
    {
        throw new NotImplementedException("TODO: devolva uma lambda p => ... (o compilador gera a árvore de expressão)");
    }
}
