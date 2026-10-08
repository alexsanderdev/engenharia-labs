using F2M07.Api.Pedidos;
using F2M07.Api.Produtos;
using Microsoft.EntityFrameworkCore;

namespace F2M07.Api.Infrastructure;

public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(b =>
        {
            b.ToTable("Produtos");
            b.HasKey(p => p.Id);
            b.Property(p => p.Sku).HasMaxLength(30).IsRequired();
            b.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            b.Property(p => p.Preco).HasPrecision(18, 2);
            // Regra que SÓ o banco garante de verdade (o provider InMemory ignora índices únicos).
            b.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<Pedido>(b =>
        {
            b.ToTable("Pedidos");
            b.HasKey(p => p.Id);
            b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(p => p.Total).HasPrecision(18, 2);
            b.HasIndex(p => p.ClienteId);
            b.HasMany(p => p.Itens).WithOne().HasForeignKey("PedidoId").OnDelete(DeleteBehavior.Cascade);
            b.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        });

        modelBuilder.Entity<ItemPedido>(b =>
        {
            b.ToTable("ItensPedido");
            b.HasKey(i => i.Id);
            b.Property(i => i.Id).ValueGeneratedNever();
            b.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
            b.Ignore(i => i.Subtotal);
            // FK real para Produtos: o banco recusa item apontando para produto inexistente.
            b.HasOne<Produto>().WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
