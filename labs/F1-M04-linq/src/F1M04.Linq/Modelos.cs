namespace F1M04.Linq;

/// <summary>Produto do catálogo do OrderFlow.</summary>
public sealed record Produto(int Id, string Sku, string Nome, string Categoria, decimal Preco, bool Ativo);

/// <summary>Cliente que faz pedidos.</summary>
public sealed record Cliente(int Id, string Nome, string Cidade);

/// <summary>Status possíveis de um pedido.</summary>
public enum StatusPedido { Criado, Confirmado, Concluido, Cancelado }

/// <summary>Item de um pedido: produto, quantidade e preço unitário no momento da compra.</summary>
public sealed record ItemPedido(int ProdutoId, int Quantidade, decimal PrecoUnitario)
{
    /// <summary>Quantidade × preço unitário.</summary>
    public decimal Subtotal => Quantidade * PrecoUnitario;
}

/// <summary>Pedido de um cliente.</summary>
public sealed record Pedido(int Id, int ClienteId, DateOnly Data, StatusPedido Status, IReadOnlyList<ItemPedido> Itens);

/// <summary>Filtro da busca no catálogo. Todos os critérios são opcionais.</summary>
public sealed record FiltroCatalogo
{
    /// <summary>Trecho do nome (sem diferenciar maiúsculas/minúsculas).</summary>
    public string? Termo { get; init; }
    public decimal? PrecoMinimo { get; init; }
    public decimal? PrecoMaximo { get; init; }
    /// <summary>Quando verdadeiro (padrão), produtos inativos são ignorados.</summary>
    public bool ApenasAtivos { get; init; } = true;
    /// <summary>Número da página, começando em 1.</summary>
    public int Pagina { get; init; } = 1;
    /// <summary>Itens por página, entre 1 e 100.</summary>
    public int TamanhoPagina { get; init; } = 10;
}

/// <summary>Uma página de resultados.</summary>
public sealed record Pagina<T>(IReadOnlyList<T> Itens, int Numero, int Tamanho, int TotalItens)
{
    /// <summary>Quantidade total de páginas (0 quando não há itens).</summary>
    public int TotalPaginas => TotalItens == 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)Tamanho);
}

/// <summary>Resumo de pedidos de um cliente.</summary>
public sealed record ResumoCliente(string NomeCliente, int QuantidadePedidos, decimal TotalGasto);

/// <summary>Produto mais vendido e a quantidade total vendida.</summary>
public sealed record ProdutoVendido(string Nome, int Quantidade);

/// <summary>Estatísticas do catálogo calculadas em uma única passada.</summary>
public sealed record EstatisticasCatalogo(int Quantidade, decimal PrecoMinimo, decimal PrecoMaximo, decimal PrecoMedio);
