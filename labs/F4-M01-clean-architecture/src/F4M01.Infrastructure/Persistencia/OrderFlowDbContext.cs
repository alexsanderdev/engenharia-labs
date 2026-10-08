using F4M01.Domain.Pedidos;
using F4M01.Domain.Produtos;
using Microsoft.EntityFrameworkCore;

namespace F4M01.Infrastructure.Persistencia;

/// <summary>DbContext do OrderFlow. Detalhe de infraestrutura: só a Infrastructure o enxerga.</summary>
public sealed class OrderFlowDbContext(DbContextOptions<OrderFlowDbContext> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderFlowDbContext).Assembly);
}
