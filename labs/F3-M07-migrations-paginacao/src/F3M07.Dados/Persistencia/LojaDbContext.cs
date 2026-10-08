using F3M07.Dados.Modelo;
using Microsoft.EntityFrameworkCore;

namespace F3M07.Dados.Persistencia;

public sealed class LojaDbContext(DbContextOptions<LojaDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(b =>
        {
            b.ToTable("Clientes");
            b.Property(c => c.Nome).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Pedido>(b =>
        {
            b.ToTable("Pedidos");
            b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            b.Property(p => p.Total).HasPrecision(18, 2);
            b.HasOne<Cliente>().WithMany().HasForeignKey(p => p.ClienteId).OnDelete(DeleteBehavior.Restrict);

            // Passo 1: rowversion — o SQL Server gera e troca o valor a cada UPDATE; o EF usa no WHERE.
            b.Property(p => p.Versao).IsRowVersion();

            // Passo 1: índice da paginação keyset, na MESMA ordem do ORDER BY (CriadoEm DESC, Id DESC)
            // e "cobrindo" as colunas da listagem (INCLUDE) para não precisar de key lookup.
            b.HasIndex(p => new { p.CriadoEm, p.Id })
                .IsDescending()
                .IncludeProperties(p => new { p.ClienteId, p.Status, p.Total })
                .HasDatabaseName("IX_Pedidos_CriadoEm_Id");

            // Passo 3: coluna nova NOT NULL com DEFAULT. O default serve para a versão ANTIGA da aplicação,
            // que ainda não conhece a coluna, continuar inserindo durante o deploy (expand/contract).
            b.Property(p => p.Canal).HasMaxLength(20).IsRequired().HasDefaultValue("Web");
        });
    }
}
