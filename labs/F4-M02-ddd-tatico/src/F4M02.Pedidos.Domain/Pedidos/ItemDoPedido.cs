using F4M02.Pedidos.Domain.Comum;
using F4M02.Pedidos.Domain.ValueObjects;

namespace F4M02.Pedidos.Domain.Pedidos;

/// <summary>
/// Entidade INTERNA ao agregado Pedido. Tem identidade (local: o produto, dentro deste pedido) e
/// ciclo de vida (a quantidade muda), mas só existe dentro de um <see cref="Pedido"/> e só é
/// criada/alterada pela raiz — por isso o construtor e <see cref="AlterarQuantidade"/> são <c>internal</c>.
/// </summary>
public sealed class ItemDoPedido : Entidade<ProdutoId>
{
    /// <summary>Copia do produto o que o pedido precisa guardar (snapshot de nome, SKU e preço).</summary>
    internal ItemDoPedido(ProdutoDoCatalogo produto, Quantidade quantidade)
        : base(produto.Id) =>
        throw new NotImplementedException("TODO (Passo 2): copie Sku, Nome e Preco do produto (snapshot) e guarde a quantidade.");

    /// <summary>Identidade local do item: o produto (um produto aparece no máximo uma vez por pedido).</summary>
    public ProdutoId ProdutoId => Id;

    public Sku Sku { get; } = null!;

    public string NomeDoProduto { get; } = string.Empty;

    /// <summary>Preço do catálogo no momento em que o item entrou. Mudanças posteriores no catálogo não afetam o pedido.</summary>
    public Dinheiro PrecoUnitario { get; } = null!;

    public Quantidade Quantidade { get; private set; } = null!;

    /// <summary>Preço unitário × quantidade. Calculado, nunca informado.</summary>
    public Dinheiro Subtotal => throw new NotImplementedException("TODO (Passo 2): PrecoUnitario * Quantidade.");

    internal void AlterarQuantidade(Quantidade novaQuantidade) =>
        throw new NotImplementedException("TODO (Passo 3): troque a quantidade (quem decide se pode é a raiz).");
}
