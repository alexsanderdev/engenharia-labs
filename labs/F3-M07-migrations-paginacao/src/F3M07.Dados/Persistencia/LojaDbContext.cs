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

            // TODO Passo 1 (ANTES de criar a migration Inicial):
            //  a) Versao como rowversion: b.Property(p => p.Versao).IsRowVersion();
            //     Sem isso o EF cria um varbinary(max) comum e NÃO usa a coluna no WHERE do UPDATE.
            //  b) Índice da paginação keyset, na ordem do ORDER BY (CriadoEm DESC, Id DESC), cobrindo
            //     ClienteId, Status e Total, com o nome "IX_Pedidos_CriadoEm_Id"
            //     (HasIndex(...).IsDescending().IncludeProperties(...).HasDatabaseName(...)).

            // TODO Passo 3 (DEPOIS da migration Inicial): troque o Ignore abaixo por
            //     b.Property(p => p.Canal).HasMaxLength(20).IsRequired().HasDefaultValue("Web");
            //  e crie a migration AdicionaCanalAoPedido (e edite-a: os pedidos antigos ganham 'Legado').
            b.Ignore(p => p.Canal);
        });
    }
}
