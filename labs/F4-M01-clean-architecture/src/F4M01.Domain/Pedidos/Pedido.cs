using F4M01.Domain.Comum;
using F4M01.Domain.Produtos;

namespace F4M01.Domain.Pedidos;

/// <summary>
/// Agregado Pedido. Regras: pelo menos um item, quantidade &gt; 0, produto inativo não entra,
/// total calculado aqui com o preço do catálogo. Nenhuma referência a EF Core, HTTP ou relógio do sistema:
/// o instante de criação chega por parâmetro (quem sabe "que horas são" é a Application, via porta).
/// </summary>
public sealed class Pedido
{
    private readonly List<ItemPedido> _itens = [];

    // Construtor sem parâmetros para o EF Core materializar (privado: ninguém de fora cria pedido vazio).
    private Pedido() { }

    public Guid Id { get; private set; }
    public Guid ClienteId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public StatusPedido Status { get; private set; }
    public decimal Total { get; private set; }
    public IReadOnlyList<ItemPedido> Itens => _itens;

    /// <summary>
    /// Fábrica do agregado: valida TODAS as regras antes de devolver um pedido.
    /// Se retornou, o pedido é válido.
    /// </summary>
    /// <exception cref="DomainException">Sem itens, quantidade &lt;= 0 ou produto inativo.</exception>
    public static Pedido Criar(Guid clienteId, DateTimeOffset criadoEm, IReadOnlyList<(Produto Produto, int Quantidade)> linhas)
    {
        ArgumentNullException.ThrowIfNull(linhas);
        if (linhas.Count == 0)
            throw new DomainException("O pedido precisa ter pelo menos um item.");

        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            CriadoEm = criadoEm,
            Status = StatusPedido.Created,
        };

        foreach (var (produto, quantidade) in linhas)
        {
            if (quantidade <= 0)
                throw new DomainException($"A quantidade de {produto.Nome} deve ser maior que zero.");
            if (!produto.Ativo)
                throw new DomainException($"O produto {produto.Nome} está inativo e não pode entrar em pedido.");

            pedido._itens.Add(new ItemPedido(produto.Id, produto.Nome, quantidade, produto.Preco));
        }

        pedido.Total = pedido._itens.Sum(i => i.Subtotal);
        return pedido;
    }
}
