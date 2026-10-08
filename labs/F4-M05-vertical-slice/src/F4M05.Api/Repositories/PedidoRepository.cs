using F4M05.Api.Models;

namespace F4M05.Api.Repositories;

// CÓDIGO INICIAL: repositórios "por tabela", com interface 1-para-1 com a classe.
// TODO (Passo 3): substitua os dois por um único Infraestrutura/BancoEmMemoria (o "DbContext" do lab),
// usado direto pelas fatias.

public interface IPedidoRepository
{
    void Adicionar(Pedido pedido);
    Pedido? ObterPorId(Guid id);
    List<Pedido> Listar();
    void Atualizar(Pedido pedido);
}

public sealed class PedidoRepository : IPedidoRepository
{
    private readonly Lock _lock = new();
    private readonly List<Pedido> _pedidos = [];

    public void Adicionar(Pedido pedido)
    {
        lock (_lock) _pedidos.Add(pedido);
    }

    public Pedido? ObterPorId(Guid id)
    {
        lock (_lock) return _pedidos.Find(p => p.Id == id);
    }

    public List<Pedido> Listar()
    {
        lock (_lock) return [.. _pedidos];
    }

    public void Atualizar(Pedido pedido)
    {
        // Em memória, nada a fazer (o objeto já foi alterado).
    }
}

public interface IProdutoRepository
{
    Produto? ObterPorId(Guid id);
}

public sealed class ProdutoRepository : IProdutoRepository
{
    private static readonly Produto[] Catalogo =
    [
        new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Nome = "Teclado", Preco = 150.00m, Ativo = true },
        new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Nome = "Mouse", Preco = 80.50m, Ativo = true },
        new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Nome = "Monitor CRT", Preco = 900.00m, Ativo = false },
    ];

    public Produto? ObterPorId(Guid id) => Array.Find(Catalogo, p => p.Id == id);
}
