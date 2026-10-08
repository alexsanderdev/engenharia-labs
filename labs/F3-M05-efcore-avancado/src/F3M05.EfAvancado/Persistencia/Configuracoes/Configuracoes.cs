using F3M05.EfAvancado.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F3M05.EfAvancado.Persistencia.Configuracoes;

/// <summary>JÁ VEM PRONTA.</summary>
public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.Property(c => c.Nome).HasMaxLength(100);
        builder.HasMany(c => c.Pedidos)
            .WithOne(p => p.Cliente)
            .HasForeignKey(p => p.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>JÁ VEM PRONTA.</summary>
public sealed class ItemPedidoConfiguration : IEntityTypeConfiguration<ItemPedido>
{
    public void Configure(EntityTypeBuilder<ItemPedido> builder)
    {
        builder.ToTable("ItensPedido");
        builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
        builder.HasOne(i => i.Produto)
            .WithMany()
            .HasForeignKey(i => i.ProdutoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

/// <summary>JÁ VEM PRONTA.</summary>
public sealed class EventoPedidoConfiguration : IEntityTypeConfiguration<EventoPedido>
{
    public void Configure(EntityTypeBuilder<EventoPedido> builder)
    {
        builder.ToTable("EventosPedido");
        builder.Property(e => e.Descricao).HasMaxLength(200);
        builder.HasIndex(e => e.OcorridoEm);
    }
}
