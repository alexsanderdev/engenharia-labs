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
        return produtos
            .Where(p => p.Ativo && string.Equals(p.Categoria, categoria, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Nome, StringComparer.Ordinal)
            .ToList();
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
        ArgumentOutOfRangeException.ThrowIfLessThan(filtro.Pagina, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(filtro.TamanhoPagina, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(filtro.TamanhoPagina, 100);

        var consulta = produtos;
        if (filtro.ApenasAtivos)
            consulta = consulta.Where(p => p.Ativo);
        if (!string.IsNullOrWhiteSpace(filtro.Termo))
            consulta = consulta.Where(p => p.Nome.Contains(filtro.Termo, StringComparison.OrdinalIgnoreCase));
        if (filtro.PrecoMinimo is { } min)
            consulta = consulta.Where(p => p.Preco >= min);
        if (filtro.PrecoMaximo is { } max)
            consulta = consulta.Where(p => p.Preco <= max);

        // Materializa uma vez: Count e Skip/Take trabalham sobre a lista, não sobre a fonte.
        var filtrados = consulta
            .OrderBy(p => p.Preco)
            .ThenBy(p => p.Nome, StringComparer.Ordinal)
            .ToList();

        var itens = filtrados
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .ToList();

        return new Pagina<Produto>(itens, filtro.Pagina, filtro.TamanhoPagina, filtrados.Count);
    }

    /// <summary>
    /// Quantidade de produtos por categoria (use <c>CountBy</c>, .NET 9+).
    /// </summary>
    public static IReadOnlyDictionary<string, int> ContarPorCategoria(IEnumerable<Produto> produtos)
    {
        return produtos.CountBy(p => p.Categoria).ToDictionary();
    }

    /// <summary>
    /// Índice de produtos por categoria (use <c>ToLookup</c>): consultar uma categoria inexistente
    /// deve devolver sequência vazia, não exceção.
    /// </summary>
    public static ILookup<string, Produto> IndicePorCategoria(IEnumerable<Produto> produtos)
    {
        return produtos.ToLookup(p => p.Categoria, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Divide os SKUs em lotes de tamanho fixo (o último pode ser menor) — use <c>Chunk</c>.
    /// Útil para enviar o catálogo a um serviço externo que aceita no máximo N itens por chamada.
    /// </summary>
    public static IReadOnlyList<string[]> SkusEmLotes(IEnumerable<Produto> produtos, int tamanhoLote)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanhoLote, 1);
        return produtos.Select(p => p.Sku).Chunk(tamanhoLote).ToList();
    }

    /// <summary>
    /// Predicado de faixa de preço como <see cref="Expression{TDelegate}"/>, para funcionar tanto
    /// com <see cref="IQueryable{T}"/> (um provider como o EF Core traduz para SQL) quanto
    /// em memória (via <c>Compile()</c>).
    /// </summary>
    public static Expression<Func<Produto, bool>> FaixaDePreco(decimal minimo, decimal maximo)
    {
        return p => p.Preco >= minimo && p.Preco <= maximo;
    }
}
