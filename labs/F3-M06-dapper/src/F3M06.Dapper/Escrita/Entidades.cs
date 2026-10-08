namespace F3M06.Dapper.Escrita;

// Modelo de ESCRITA (EF Core). Neste lab ele já vem pronto e propositalmente simples:
// o foco é o lado de LEITURA (Dapper), que consulta as MESMAS tabelas com SQL próprio.

/// <summary>Status do pedido, gravado como texto (nvarchar) pelo EF Core.</summary>
public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}

public sealed class Cliente
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
}

public sealed class Produto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
    public string Nome { get; set; } = "";
    public decimal Preco { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class Pedido
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public DateTime CriadoEm { get; set; }
    public StatusPedido Status { get; set; } = StatusPedido.Created;
    public decimal Total { get; set; }
    public List<ItemPedido> Itens { get; set; } = [];

    /// <summary>Adiciona um item com o preço atual do produto e recalcula o total (no servidor).</summary>
    public void AdicionarItem(Produto produto, int quantidade)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantidade);
        if (!produto.Ativo)
            throw new InvalidOperationException($"Produto inativo não entra em pedido: {produto.Sku}");

        Itens.Add(new ItemPedido { ProdutoId = produto.Id, Quantidade = quantidade, PrecoUnitario = produto.Preco });
        Total = Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
    }
}

public sealed class ItemPedido
{
    /// <summary>Identity (int) gerado pelo banco.</summary>
    public int Id { get; set; }
    public Guid PedidoId { get; set; }
    public Pedido? Pedido { get; set; }
    public Guid ProdutoId { get; set; }
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
