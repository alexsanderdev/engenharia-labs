using F3M05.EfAvancado.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F3M05.EfAvancado.Persistencia.Configuracoes;

/// <summary>
/// Mapeamento de <see cref="Pedido"/>. Exclusão lógica: um <b>global query filter</b> esconde
/// pedidos com <c>Excluido = 1</c> de TODAS as consultas (inclusive via navegação/Include).
/// </summary>
public sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(p => new { p.Status, p.CriadoEm });

        builder.HasMany(p => p.Itens)
            .WithOne(i => i.Pedido)
            .HasForeignKey(i => i.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Eventos)
            .WithOne()
            .HasForeignKey(e => e.PedidoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(p => !p.Excluido);
    }
}
