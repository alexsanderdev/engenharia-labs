using System.Collections.Concurrent;

namespace F5M01.Api.Dominio;

/// <summary>Produtos fixos do catálogo usados no lab e nos testes.</summary>
public static class ProdutosConhecidos
{
    public static readonly Produto Teclado = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Teclado mecânico", 250.00m, Ativo: true, Custo: 140.00m);
    public static readonly Produto Mouse = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mouse sem fio", 120.00m, Ativo: true, Custo: 55.00m);
    public static readonly Produto Monitor = new(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Monitor 27\"", 1500.00m, Ativo: true, Custo: 980.00m);
    public static readonly Produto Webcam = new(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Webcam HD", 300.00m, Ativo: false, Custo: 160.00m);
}

public interface ICatalogo
{
    Produto? Obter(Guid produtoId);
}

public interface IPedidoRepositorio
{
    Pedido? Obter(Guid pedidoId);
    IReadOnlyList<Pedido> Listar();
    void Adicionar(Pedido pedido);
}

/// <summary>Catálogo em memória (PRONTO).</summary>
public sealed class CatalogoEmMemoria : ICatalogo
{
    private readonly Dictionary<Guid, Produto> _produtos = new[]
    {
        ProdutosConhecidos.Teclado, ProdutosConhecidos.Mouse, ProdutosConhecidos.Monitor, ProdutosConhecidos.Webcam,
    }.ToDictionary(p => p.Id);

    public Produto? Obter(Guid produtoId) => _produtos.GetValueOrDefault(produtoId);
}

/// <summary>
/// Repositório em memória (PRONTO). Singleton: a "persistência" vive enquanto a aplicação vive.
/// Como devolve a própria instância, alterar o agregado já "salva" — num banco real você chamaria SaveChanges
/// e usaria a versão como token de concorrência (rowversion) além do If-Match.
/// </summary>
public sealed class PedidoRepositorioEmMemoria : IPedidoRepositorio
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public Pedido? Obter(Guid pedidoId) => _pedidos.GetValueOrDefault(pedidoId);

    public IReadOnlyList<Pedido> Listar() => [.. _pedidos.Values];

    public void Adicionar(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        _pedidos[pedido.Id] = pedido;
    }
}
