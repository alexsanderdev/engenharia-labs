using F4M06.Pedidos.Dominio;
using Microsoft.EntityFrameworkCore;

namespace F4M06.Pedidos.Infra;

/// <summary>DbContext PRIVADO de Pedidos: só enxerga as tabelas do schema <c>pedidos</c>.</summary>
public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public const string Schema = "pedidos";

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>(pedido =>
        {
            pedido.ToTable("Pedidos");
            pedido.HasKey(p => p.Id);
            pedido.Property(p => p.Id).ValueGeneratedNever();
            pedido.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            pedido.Ignore(p => p.Total);
            pedido.HasIndex(p => p.ClienteId);

            pedido.OwnsMany(p => p.Itens, item =>
            {
                item.ToTable("ItensPedido");
                item.WithOwner().HasForeignKey("PedidoId");
                item.Property<int>("Id").ValueGeneratedOnAdd();
                item.HasKey("Id");
                item.Property(i => i.NomeProduto).HasMaxLength(200).IsRequired();
                item.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
                item.Ignore(i => i.Subtotal);
            });
            pedido.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
        });
    }
}
