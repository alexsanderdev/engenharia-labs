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
        ArgumentNullException.ThrowIfNull(politica);

        if (linhas is not { Count: > 0 })
        {
            throw new PedidoInvalidoException("Pedido precisa de ao menos um item");
        }

        if (linhas.Any(l => l.Quantidade <= 0))
        {
            throw new PedidoInvalidoException("Quantidade deve ser maior que zero");
        }

        var inativo = linhas.FirstOrDefault(l => !l.Produto.Ativo);
        if (inativo is not null)
        {
            throw new PedidoInvalidoException($"Produto inativo: {inativo.Produto.Nome}");
        }

        var itens = linhas.Select(l => new ItemPedido(l.Produto.Id, l.Produto.Nome, l.Produto.Preco, l.Quantidade)).ToList();
        var subtotal = itens.Sum(i => i.Subtotal);

        return new Pedido(Guid.NewGuid(), clienteId, itens, politica.CalcularDesconto(subtotal));
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
