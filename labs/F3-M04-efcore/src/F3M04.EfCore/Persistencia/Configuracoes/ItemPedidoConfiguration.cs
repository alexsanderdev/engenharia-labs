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
        builder.ToTable("ItensPedido");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        builder.Ignore(i => i.Subtotal);

        builder.HasOne<Produto>()
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
