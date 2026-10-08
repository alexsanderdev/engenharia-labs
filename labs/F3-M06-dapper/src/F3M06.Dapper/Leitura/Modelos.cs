using F3M06.Dapper.Escrita;

namespace F3M06.Dapper.Leitura;

// Read models: DTOs "achatados", no formato que a TELA/API precisa — não são as entidades.
// O Dapper preenche as propriedades pelo NOME da coluna (o alias no SELECT), sem diferenciar maiúsculas.
// Por isso todas têm setter (init) e valor padrão, e nenhuma usa "required".

/// <summary>Produto para listagens e buscas.</summary>
public sealed record ProdutoResumo
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = "";
    public string Nome { get; init; } = "";
    public decimal Preco { get; init; }
    public bool Ativo { get; init; }
}

/// <summary>Cabeçalho do pedido + itens (montado com multi-mapping).</summary>
public sealed record PedidoDetalhe
{
    public Guid Id { get; init; }
    public Guid ClienteId { get; init; }
    public string ClienteNome { get; init; } = "";
    public DateTime CriadoEm { get; init; }
    public StatusPedido Status { get; init; }
    public decimal Total { get; init; }

    /// <summary>Preenchida pelo multi-mapping (mutável de propósito: o mapeamento vai adicionando).</summary>
    public List<ItemDetalhe> Itens { get; init; } = [];
}

/// <summary>Linha de item com dados do produto (JOIN com Produtos).</summary>
public sealed record ItemDetalhe
{
    public int ItemId { get; init; }
    public Guid ProdutoId { get; init; }
    public string Sku { get; init; } = "";
    public string NomeProduto { get; init; } = "";
    public int Quantidade { get; init; }
    public decimal PrecoUnitario { get; init; }

    /// <summary>Calculada em C# (não vem do banco).</summary>
    public decimal Subtotal => Quantidade * PrecoUnitario;
}

/// <summary>Uma linha de listagem de pedidos (sem itens).</summary>
public sealed record PedidoResumo
{
    public Guid Id { get; init; }
    public string ClienteNome { get; init; } = "";
    public DateTime CriadoEm { get; init; }
    public StatusPedido Status { get; init; }
    public decimal Total { get; init; }
}

public sealed record ClienteResumo
{
    public Guid Id { get; init; }
    public string Nome { get; init; } = "";
    public string Email { get; init; } = "";
}

/// <summary>Números agregados do cliente. Pedidos cancelados NÃO contam.</summary>
public sealed record EstatisticasDoCliente
{
    public int QuantidadePedidos { get; init; }
    public decimal TotalGasto { get; init; }
    public DateTime? UltimoPedidoEm { get; init; }
}

/// <summary>Tela "Minha conta": três consultas, uma ida ao banco (QueryMultiple).</summary>
public sealed record PainelDoCliente(
    ClienteResumo Cliente,
    IReadOnlyList<PedidoResumo> UltimosPedidos,
    EstatisticasDoCliente Estatisticas);

/// <summary>Uma linha do relatório de vendas (um dia). Pedidos cancelados não entram.</summary>
public sealed record VendasPorDia
{
    public DateTime Dia { get; init; }
    public int QuantidadePedidos { get; init; }
    public int ItensVendidos { get; init; }
    public decimal Faturamento { get; init; }

    /// <summary>Faturamento / pedidos, arredondado em 2 casas (calculado em C#).</summary>
    public decimal TicketMedio => QuantidadePedidos == 0 ? 0 : Math.Round(Faturamento / QuantidadePedidos, 2);
}

/// <summary>Página de resultados com o total para a UI montar a navegação.</summary>
public sealed record Pagina<T>(IReadOnlyList<T> Itens, int NumeroPagina, int TamanhoPagina, int TotalItens)
{
    public int TotalPaginas => TotalItens == 0 ? 0 : (int)Math.Ceiling(TotalItens / (double)TamanhoPagina);
}
