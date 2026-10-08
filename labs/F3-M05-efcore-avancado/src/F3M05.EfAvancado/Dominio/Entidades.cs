namespace F3M05.EfAvancado.Dominio;

/// <summary>Entidade com datas de auditoria preenchidas automaticamente no SaveChanges.</summary>
public interface IAuditavel
{
    DateTimeOffset CriadoEm { get; set; }
    DateTimeOffset AtualizadoEm { get; set; }
}

/// <summary>Entidade que nunca é apagada de verdade: "remover" marca <see cref="Excluido"/>.</summary>
public interface IExclusaoLogica
{
    bool Excluido { get; set; }
}

public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}

public sealed class Cliente
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public List<Pedido> Pedidos { get; set; } = [];
}

public sealed class Produto : IAuditavel
{
    public int Id { get; set; }
    public required Sku Sku { get; set; }
    public required string Nome { get; set; }
    public required Dinheiro Preco { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
}

public sealed class Pedido : IAuditavel, IExclusaoLogica
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
    public StatusPedido Status { get; set; } = StatusPedido.Created;
    public DateTimeOffset CriadoEm { get; set; }
    public DateTimeOffset AtualizadoEm { get; set; }
    public bool Excluido { get; set; }
    public List<ItemPedido> Itens { get; set; } = [];
    public List<EventoPedido> Eventos { get; set; } = [];
}

public sealed class ItemPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public Pedido Pedido { get; set; } = null!;
    public int ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

/// <summary>Histórico do pedido ("criado", "pago", "separado"...). Cresce rápido: candidato a expurgo.</summary>
public sealed class EventoPedido
{
    public int Id { get; set; }
    public int PedidoId { get; set; }
    public required string Descricao { get; set; }
    public DateTimeOffset OcorridoEm { get; set; }
}
