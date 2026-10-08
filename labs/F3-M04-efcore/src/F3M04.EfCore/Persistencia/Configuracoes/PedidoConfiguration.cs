using F3M04.EfCore.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F3M04.EfCore.Persistencia.Configuracoes;

/// <summary>
/// Mapeamento de <see cref="Pedido"/>: tabela <c>Pedidos</c>; <c>Status</c> gravado como texto
/// (até 20); <c>Total</c> decimal(18,2); FK para <c>Clientes</c> sem cascade (Restrict);
/// índice em <c>ClienteId</c>; um-para-muitos com os itens via FK sombra <c>PedidoId</c>
/// (obrigatória, cascade: item não vive sem pedido).
/// </summary>
public sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Total).HasPrecision(18, 2);

        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.ClienteId);

        builder.HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey("PedidoId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        // O EF escreve no campo _itens; a propriedade pública é só leitura.
        builder.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
