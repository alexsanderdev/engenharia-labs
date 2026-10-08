using F2M08.Domain.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F2M08.Infrastructure.Persistencia;

/// <summary>
/// Mapeamento do Pedido: detalhe de persistência, mora na Infrastructure.
/// (No código inicial ele estava DENTRO da entidade, arrastando EF Core para o domínio.)
/// </summary>
public sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => p.ClienteId);
        builder.Ignore(p => p.Unidades);
        builder.Ignore(p => p.Subtotal);
        builder.Ignore(p => p.Desconto);
        builder.Ignore(p => p.Total);

        builder.OwnsMany(p => p.Itens, item =>
        {
            item.ToTable("ItensPedido");
            item.WithOwner().HasForeignKey("PedidoId");
            item.Property<int>("ItemId");
            item.HasKey("ItemId");
            item.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
            item.Ignore(i => i.Subtotal);
        });
        builder.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
