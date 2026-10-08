using System.Collections.Concurrent;

namespace F5M02.Api.Dominio;

public enum StatusPedido { Created, Confirmed, Completed, Cancelled }

public sealed record Produto(Guid Id, string Nome, decimal Preco, bool Ativo);

public sealed record ItemPedido(Guid ProdutoId, string NomeProduto, decimal PrecoUnitario, int Quantidade)
{
    public decimal Subtotal => PrecoUnitario * Quantidade;
}

/// <summary>Pedido (PRONTO e propositalmente simples: o foco do lab é a borda HTTP, não o domínio).</summary>
public sealed class Pedido
{
    public required Guid Id { get; init; }
    public required Guid ClienteId { get; init; }
    public required DateTimeOffset CriadoEm { get; init; }
    public StatusPedido Status { get; set; } = StatusPedido.Created;
    public required IReadOnlyList<ItemPedido> Itens { get; init; }
    public decimal Total => Itens.Sum(i => i.Subtotal);
}

public static class ProdutosConhecidos
{
    public static readonly Produto Teclado = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Teclado mecânico", 250.00m, Ativo: true);
    public static readonly Produto Mouse = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mouse sem fio", 120.00m, Ativo: true);
    public static readonly Produto Webcam = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Webcam HD", 300.00m, Ativo: false);

    public static readonly IReadOnlyDictionary<Guid, Produto> Todos =
        new[] { Teclado, Mouse, Webcam }.ToDictionary(p => p.Id);
}

/// <summary>Repositório em memória (PRONTO) com um pedido de exemplo já cadastrado.</summary>
public sealed class PedidoRepositorio
{
    /// <summary>Pedido semeado: 2 teclados + 1 mouse = 620,00, status Confirmed.</summary>
    public static readonly Guid PedidoExemploId = Guid.Parse("0f0f0f0f-0000-0000-0000-000000000001");
    public static readonly Guid ClienteExemploId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public PedidoRepositorio()
    {
        Adicionar(new Pedido
        {
            Id = PedidoExemploId,
            ClienteId = ClienteExemploId,
            CriadoEm = new DateTimeOffset(2026, 3, 2, 12, 0, 0, TimeSpan.Zero),
            Status = StatusPedido.Confirmed,
            Itens =
            [
                new(ProdutosConhecidos.Teclado.Id, ProdutosConhecidos.Teclado.Nome, ProdutosConhecidos.Teclado.Preco, 2),
                new(ProdutosConhecidos.Mouse.Id, ProdutosConhecidos.Mouse.Nome, ProdutosConhecidos.Mouse.Preco, 1),
            ],
        });
    }

    public Pedido? Obter(Guid id) => _pedidos.GetValueOrDefault(id);

    public void Adicionar(Pedido pedido) => _pedidos[pedido.Id] = pedido;
}
