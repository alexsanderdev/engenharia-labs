using Microsoft.EntityFrameworkCore;

namespace F3M06.Dapper.Escrita;

/// <summary>
/// Lado de escrita do "CQRS leve": EF Core cuida de inserir/alterar com regras e transação.
/// O lado de leitura (pasta Leitura) usa Dapper sobre as mesmas tabelas.
/// </summary>
public sealed class LojaDbContext(DbContextOptions<LojaDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(b =>
        {
            b.ToTable("Clientes");
            b.Property(c => c.Nome).HasMaxLength(100).IsRequired();
            b.Property(c => c.Email).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Produto>(b =>
        {
            b.ToTable("Produtos");
            b.Property(p => p.Sku).HasMaxLength(30).IsRequired();
            b.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            b.Property(p => p.Preco).HasPrecision(18, 2);
            b.HasIndex(p => p.Sku).IsUnique();
        });

        modelBuilder.Entity<Pedido>(b =>
        {
            b.ToTable("Pedidos");
            b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(p => p.Total).HasPrecision(18, 2);
            b.HasOne<Cliente>().WithMany().HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.Restrict);
            b.HasMany(p => p.Itens).WithOne(i => i.Pedido).HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
            // Índice das leituras por cliente e por período (relatório e paginação).
            b.HasIndex(p => new { p.ClienteId, p.CriadoEm });
            b.HasIndex(p => p.CriadoEm);
        });

        modelBuilder.Entity<ItemPedido>(b =>
        {
            b.ToTable("ItensPedido");
            b.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
            b.HasOne<Produto>().WithMany().HasForeignKey(i => i.ProdutoId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
