namespace F4M05.Api.DTOs;

// CÓDIGO INICIAL: todos os DTOs de todas as telas numa pasta só.
// Mudar o PedidoDto "para a tela de detalhes" muda também a resposta de criar, confirmar e cancelar.
// TODO (Passo 4): cada fatia passa a ter o SEU Command/Query e o SEU Response.

public sealed class CriarPedidoDto
{
    public Guid ClienteId { get; set; }
    public List<ItemPedidoDto>? Itens { get; set; }
}

public sealed class ItemPedidoDto
{
    public Guid ProdutoId { get; set; }
    public int Quantidade { get; set; }
}

public sealed class PedidoDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Status { get; set; } = "";
    public decimal Total { get; set; }
    public List<ItemPedidoRespostaDto> Itens { get; set; } = [];
}

public sealed class ItemPedidoRespostaDto
{
    public Guid ProdutoId { get; set; }
    public string Nome { get; set; } = "";
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public sealed class PedidoResumoDto
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public string Status { get; set; } = "";
    public decimal Total { get; set; }
}
