namespace F4M05.Api.Models;

// CÓDIGO INICIAL (camadas horizontais). Modelo anêmico: só dados; as regras estão no PedidoService.
// TODO (Passo 2): mova para Dominio/ e transforme em domínio rico — Pedido.Criar, Confirmar(), Cancelar()
// e Status com setter privado. As fatias Confirmar/Cancelar chamam o domínio; a regra não é copiada em cada fatia.

public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}

public sealed class Produto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public decimal Preco { get; set; }
    public bool Ativo { get; set; }
}

public sealed class ItemPedido
{
    public Guid ProdutoId { get; set; }
    public string Nome { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}

public sealed class Pedido
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public StatusPedido Status { get; set; }
    public List<ItemPedido> Itens { get; set; } = [];
}
