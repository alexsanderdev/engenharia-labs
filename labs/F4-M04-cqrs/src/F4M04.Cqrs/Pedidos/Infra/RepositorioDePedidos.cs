using F4M04.Cqrs.Pedidos.Dominio;

namespace F4M04.Cqrs.Pedidos.Infra;

/// <summary>Repositório do agregado Pedido (lado de escrita).</summary>
public interface IRepositorioDePedidos
{
    Task<Pedido?> ObterAsync(Guid id, CancellationToken ct);
    void Adicionar(Pedido pedido);
}

/// <summary>Catálogo consultado pelo lado de escrita para obter preço e situação do produto.</summary>
public interface ICatalogo
{
    Task<Produto?> ObterProdutoAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Repositório em memória com "change tracking" simplificado (scoped): tudo que foi
/// adicionado ou carregado fica rastreado até a unidade de trabalho confirmar.
/// Pedidos adicionados NÃO aparecem no banco antes do commit.
/// </summary>
public sealed class RepositorioDePedidos(BancoDeEscrita banco) : IRepositorioDePedidos, ICatalogo
{
    private readonly HashSet<Pedido> _rastreados = [];

    internal IReadOnlyCollection<Pedido> Rastreados => _rastreados;

    public Task<Pedido?> ObterAsync(Guid id, CancellationToken ct)
    {
        var pedido = _rastreados.FirstOrDefault(p => p.Id == id)
                     ?? (banco.Pedidos.TryGetValue(id, out var salvo) ? salvo : null);
        if (pedido is not null) _rastreados.Add(pedido);
        return Task.FromResult(pedido);
    }

    public void Adicionar(Pedido pedido) => _rastreados.Add(pedido);

    public Task<Produto?> ObterProdutoAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(banco.Produtos.TryGetValue(id, out var produto) ? produto : null);

    internal void LimparRastreamento() => _rastreados.Clear();
}
