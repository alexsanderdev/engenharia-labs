using F2M08.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace F2M08.Infrastructure.Persistencia;

/// <summary>DbContext do lab (não é usado pelos testes: ele existe para mostrar ONDE o mapeamento mora).</summary>
public sealed class OrderFlowDbContext(DbContextOptions<OrderFlowDbContext> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO (Passo 1): troque por modelBuilder.ApplyConfiguration(new PedidoConfiguration()).
        Pedido.ConfigurarMapeamento(modelBuilder);

        modelBuilder.Entity<Produto>(b =>
        {
            b.ToTable("Produtos");
            b.HasKey(p => p.Id);
            b.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            b.Property(p => p.Preco).HasPrecision(18, 2);
        });
    }
}
