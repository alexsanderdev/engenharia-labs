using F2M02.Solid.Descontos;

namespace F2M02.Solid.Dominio;

public sealed record Produto(Guid Id, string Nome, decimal Preco, bool Ativo);

public sealed record ItemPedido(Guid ProdutoId, string NomeProduto, decimal PrecoUnitario, int Quantidade)
{
    public decimal Subtotal => PrecoUnitario * Quantidade;
}

/// <summary>Uma linha pedida pelo cliente, já com o produto carregado do catálogo.</summary>
public sealed record LinhaDoPedido(Produto Produto, int Quantidade);

public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}

public sealed class PedidoInvalidoException(string message) : Exception(message);

/// <summary>
/// Entidade Pedido. A REGRA de criação (validação + total + desconto) mora aqui: é o "motivo
/// de mudança" do negócio, separado de persistência, e-mail e mensageria (SRP).
/// Não tem interface: não há variação nem necessidade de substituir em teste.
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens;

    internal Pedido(Guid id, Guid clienteId, IEnumerable<ItemPedido> itens, decimal desconto)
    {
        Id = id;
        ClienteId = clienteId;
        _itens = [.. itens];
        Desconto = desconto;
    }

    public Guid Id { get; }
    public Guid ClienteId { get; }
    public IReadOnlyList<ItemPedido> Itens => _itens;
    public decimal Subtotal => _itens.Sum(i => i.Subtotal);
    public decimal Desconto { get; }
    public decimal Total => Subtotal - Desconto;
    public StatusPedido Status { get; private set; } = StatusPedido.Created;

    /// <summary>
    /// Cria um pedido novo (status Created). Validações, nesta ordem, com <see cref="PedidoInvalidoException"/>:
    /// sem linhas → "Pedido precisa de ao menos um item"; alguma quantidade ≤ 0 → "Quantidade deve ser maior que zero";
    /// produto inativo → "Produto inativo: {Nome}". O desconto vem da <paramref name="politica"/> aplicada ao subtotal.
    /// </summary>
    public static Pedido Criar(Guid clienteId, IReadOnlyList<LinhaDoPedido> linhas, IPoliticaDeDesconto politica)
    {
        // Mova para cá as etapas 1 e 3 do PedidoService (validação e desconto). Sem I/O aqui.
        throw new NotImplementedException("TODO: valide as linhas, monte os ItemPedido e aplique a política de desconto ao subtotal");
    }

    public void Cancelar()
    {
        if (Status == StatusPedido.Completed)
        {
            throw new PedidoInvalidoException("Pedido concluído não pode ser cancelado");
        }

        Status = StatusPedido.Cancelled;
    }
}
