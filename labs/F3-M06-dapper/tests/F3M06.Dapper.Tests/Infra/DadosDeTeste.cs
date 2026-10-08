using F3M06.Dapper.Escrita;

namespace F3M06.Dapper.Tests.Infra;

/// <summary>
/// Massa de dados determinística, gravada UMA vez pelo EF Core (lado de escrita).
/// Os testes de leitura comparam o resultado do Dapper com estes objetos em memória ("oráculo").
/// </summary>
public sealed class DadosDeTeste
{
    public static readonly Guid AnaId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid BrunoId = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    public static readonly Guid CarlaId = Guid.Parse("c0000000-0000-0000-0000-000000000003");

    /// <summary>Pedido Completed de 02/03 com 2 itens: Teclado ×1 (250) + Mouse ×2 (120) = 490.</summary>
    public static readonly Guid PedidoAnaComDoisItensId = Guid.Parse("a1000000-0000-0000-0000-000000000001");

    /// <summary>Pedido Cancelled de 02/03 (Monitor, 1500): não pode entrar em relatório nem estatística.</summary>
    public static readonly Guid PedidoAnaCanceladoId = Guid.Parse("a1000000-0000-0000-0000-000000000002");

    /// <summary>Pedido "rascunho" sem nenhum item (Created, total 0).</summary>
    public static readonly Guid PedidoAnaSemItensId = Guid.Parse("a1000000-0000-0000-0000-000000000004");

    public List<Cliente> Clientes { get; } = [];
    public List<Produto> Produtos { get; } = [];
    public List<Pedido> Pedidos { get; } = [];

    public Produto Teclado { get; }
    public Produto Mouse { get; }
    public Produto Monitor { get; }
    public Produto Cabo { get; }
    public Produto Suporte { get; }
    public Produto WebcamInativa { get; }

    public DadosDeTeste()
    {
        Clientes.AddRange(
        [
            new Cliente { Id = AnaId, Nome = "Ana Souza", Email = "ana@orderflow.dev" },
            new Cliente { Id = BrunoId, Nome = "Bruno Lima", Email = "bruno@orderflow.dev" },
            new Cliente { Id = CarlaId, Nome = "Carla Dias", Email = "carla@orderflow.dev" }, // sem pedidos
        ]);

        Teclado = NovoProduto(1, "TEC-001", "Teclado Mecânico", 250.00m);
        Mouse = NovoProduto(2, "MOU-001", "Mouse Sem Fio", 120.00m);
        Monitor = NovoProduto(3, "MON-001", "Monitor 27 polegadas", 1500.00m);
        Cabo = NovoProduto(4, "CAB-001", "Cabo HDMI", 40.00m);
        Suporte = NovoProduto(5, "SUP-001", "Suporte D'Angelo", 89.90m); // apóstrofo no nome!
        WebcamInativa = NovoProduto(6, "WEB-001", "Webcam HD com Microfone", 300.00m, ativo: false);

        // Ana: 4 pedidos em março + 1 no último segundo de março.
        NovoPedido(PedidoAnaComDoisItensId, AnaId, new DateTime(2026, 3, 2, 10, 0, 0), StatusPedido.Completed, (Teclado, 1), (Mouse, 2));
        NovoPedido(PedidoAnaCanceladoId, AnaId, new DateTime(2026, 3, 2, 15, 30, 0), StatusPedido.Cancelled, (Monitor, 1));
        NovoPedido(Guid.Parse("a1000000-0000-0000-0000-000000000003"), AnaId, new DateTime(2026, 3, 5, 9, 30, 0), StatusPedido.Confirmed, (Cabo, 3));
        NovoPedido(PedidoAnaSemItensId, AnaId, new DateTime(2026, 3, 6, 11, 0, 0), StatusPedido.Created);
        NovoPedido(Guid.Parse("a1000000-0000-0000-0000-000000000005"), AnaId, new DateTime(2026, 3, 31, 23, 59, 59), StatusPedido.Completed, (Suporte, 2));

        // Bruno: 20 pedidos a cada 7 h a partir de 03/03 08:00; um em cada cinco é cancelado.
        for (var i = 0; i < 20; i++)
        {
            NovoPedido(
                Guid.Parse($"b1000000-0000-0000-0000-{i:D12}"),
                BrunoId,
                new DateTime(2026, 3, 3, 8, 0, 0).AddHours(7 * i),
                i % 5 == 4 ? StatusPedido.Cancelled : StatusPedido.Completed,
                (Mouse, 1), (Cabo, i % 3 + 1));
        }

        // Bruno: primeiro segundo de abril (fora de um relatório de março).
        NovoPedido(Guid.Parse("b2000000-0000-0000-0000-000000000001"), BrunoId, new DateTime(2026, 4, 1, 0, 0, 0), StatusPedido.Completed, (Teclado, 1));
    }

    private Produto NovoProduto(int n, string sku, string nome, decimal preco, bool ativo = true)
    {
        var produto = new Produto { Id = Guid.Parse($"d0000000-0000-0000-0000-{n:D12}"), Sku = sku, Nome = nome, Preco = preco, Ativo = ativo };
        Produtos.Add(produto);
        return produto;
    }

    private void NovoPedido(Guid id, Guid clienteId, DateTime criadoEm, StatusPedido status, params (Produto Produto, int Quantidade)[] itens)
    {
        var pedido = new Pedido { Id = id, ClienteId = clienteId, CriadoEm = criadoEm };
        foreach (var (produto, quantidade) in itens)
            pedido.AdicionarItem(produto, quantidade);
        pedido.Status = status;
        Pedidos.Add(pedido);
    }

    public Cliente Cliente(Guid id) => Clientes.Single(c => c.Id == id);
}
