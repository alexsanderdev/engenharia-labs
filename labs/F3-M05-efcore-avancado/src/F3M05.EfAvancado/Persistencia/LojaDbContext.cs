using F3M05.EfAvancado.Dominio;
using Microsoft.EntityFrameworkCore;

namespace F3M05.EfAvancado.Persistencia;

/// <summary>DbContext da loja. Os interceptadores são registrados nas options (ver os testes).</summary>
public sealed class LojaDbContext(DbContextOptions<LojaDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();
    public DbSet<EventoPedido> EventosPedido => Set<EventoPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LojaDbContext).Assembly);
}
