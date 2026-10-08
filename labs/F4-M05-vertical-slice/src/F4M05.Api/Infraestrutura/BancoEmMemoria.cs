using F4M05.Api.Dominio;

namespace F4M05.Api.Infraestrutura;

/// <summary>
/// "DbContext" do lab: armazenamento em memória, thread-safe, registrado como singleton.
/// As fatias usam isto DIRETO (como usariam um DbContext): sem repositório genérico no meio.
/// Num sistema real, aqui estaria o <c>OrderFlowDbContext</c> do EF Core.
/// </summary>
public sealed class BancoEmMemoria
{
    private readonly Lock _lock = new();
    private readonly List<Pedido> _pedidos = [];
    private readonly Dictionary<Guid, Produto> _produtos;

    public BancoEmMemoria()
    {
        // Catálogo semente (ids fixos: os testes usam os mesmos valores).
        _produtos = new[]
        {
            new Produto(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Teclado", 150.00m, ativo: true),
            new Produto(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mouse", 80.50m, ativo: true),
            new Produto(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Monitor CRT", 900.00m, ativo: false),
        }.ToDictionary(p => p.Id);
    }

    public IReadOnlyList<Produto> ObterProdutos(IEnumerable<Guid> ids) =>
        [.. ids.Distinct().Where(_produtos.ContainsKey).Select(id => _produtos[id])];

    public void AdicionarPedido(Pedido pedido)
    {
        lock (_lock) _pedidos.Add(pedido);
    }

    public Pedido? ObterPedido(Guid id)
    {
        lock (_lock) return _pedidos.Find(p => p.Id == id);
    }

    public IReadOnlyList<Pedido> ListarPedidos(Guid? clienteId)
    {
        lock (_lock) return [.. _pedidos.Where(p => clienteId is null || p.ClienteId == clienteId)];
    }

    /// <summary>Em memória, as mudanças já estão aplicadas. Num DbContext seria <c>SaveChangesAsync</c>.</summary>
    public Task SalvarAsync(CancellationToken ct) => Task.CompletedTask;
}
