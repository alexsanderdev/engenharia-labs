using F6M04.Pedidos.Dominio;
using F6M04.Pedidos.Outbox;
using Microsoft.EntityFrameworkCore;

namespace F6M04.Pedidos.Infra;

/// <summary>
/// PRONTO. Banco do contexto de Pedidos: o agregado E a tabela de Outbox moram no MESMO banco — é isso que
/// permite gravá-los na mesma transação local (sem transação distribuída).
/// </summary>
public sealed class PedidosDbContext(DbContextOptions<PedidosDbContext> options) : DbContext(options)
{
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pedido>(p =>
        {
            p.ToTable("Pedidos");
            p.HasKey(x => x.Id);
            p.Property(x => x.Numero).HasMaxLength(20).IsRequired();
            p.HasIndex(x => x.Numero).IsUnique();
            p.Property(x => x.ClienteEmail).HasMaxLength(200).IsRequired();
            p.Property(x => x.Total).HasPrecision(18, 2);
            p.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            p.Property(x => x.Versao).IsConcurrencyToken(); // dois "confirmar" simultâneos: um perde (DbUpdateConcurrencyException)
            p.Ignore(x => x.Eventos);
        });

        modelBuilder.Entity<OutboxMessage>(o =>
        {
            o.ToTable("OutboxMessages");

            // Id (MessageId) é a chave lógica, mas NÃO clusterizada: GUIDs fragmentariam o índice.
            o.HasKey(x => x.Id).IsClustered(false);

            // Sequencia (IDENTITY) é o índice clusterizado: inserções no fim e leitura em ordem de gravação.
            o.Property(x => x.Sequencia).UseIdentityColumn();
            o.HasIndex(x => x.Sequencia).IsUnique().IsClustered();

            // A história de cada agregado: uma versão por evento, sem buracos nem repetição. Também apoia a regra
            // "existe evento ANTERIOR pendente deste pedido?".
            o.HasIndex(x => new { x.ChaveDeOrdenacao, x.VersaoDoAgregado }).IsUnique();

            o.Property(x => x.Tipo).HasMaxLength(100).IsRequired();
            o.Property(x => x.ChaveDeOrdenacao).HasMaxLength(100).IsRequired();
            o.Property(x => x.Payload).IsRequired();
            o.Property(x => x.UltimoErro).HasMaxLength(2000);
        });
    }
}
