using F3M04.EfCore.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F3M04.EfCore.Persistencia.Configuracoes;

/// <summary>
/// Mapeamento de <see cref="ItemPedido"/>: tabela <c>ItensPedido</c>; <c>PrecoUnitario</c>
/// decimal(18,2); FK para <c>Produtos</c> com Restrict (apagar produto já vendido é erro);
/// <c>Subtotal</c> não é coluna.
/// </summary>
public sealed class ItemPedidoConfiguration : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> builder)
    {
        // TODO (Passo 2): tabela "ItensPedido"; chave Id gerada pelo domínio; PrecoUnitario 18,2;
        //                 Subtotal ignorado (explícito, mesmo que a convenção já ignore).
        // TODO (Passo 3): FK ProdutoId → Produtos (HasOne<Produto>().WithMany()...) com Restrict.
    }
}
