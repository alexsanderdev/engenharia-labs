using F6M04.Notificacoes.Dominio;
using F6M04.Notificacoes.Inbox;
using Microsoft.EntityFrameworkCore;

namespace F6M04.Notificacoes.Infra;

/// <summary>
/// PRONTO. Banco do serviço de Notificações (outro banco, outro dono). A Inbox mora AQUI, junto do efeito
/// colateral, para os dois serem gravados na mesma transação local.
/// </summary>
public sealed class NotificacoesDbContext(DbContextOptions<NotificacoesDbContext> options) : DbContext(options)
{
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notificacao>(n =>
        {
            n.ToTable("Notificacoes");
            n.HasKey(x => x.Id);
            n.Property(x => x.Tipo).HasMaxLength(100).IsRequired();
            n.Property(x => x.Destinatario).HasMaxLength(200).IsRequired();
            n.Property(x => x.Texto).HasMaxLength(1000).IsRequired();
            n.HasIndex(x => x.PedidoId);
        });

        modelBuilder.Entity<InboxMessage>(i =>
        {
            i.ToTable("InboxMessages");
            i.HasKey(x => new { x.MessageId, x.Consumidor }); // a deduplicação é garantida pelo BANCO
            i.Property(x => x.MessageId).HasMaxLength(100);
            i.Property(x => x.Consumidor).HasMaxLength(100);
            i.Property(x => x.Tipo).HasMaxLength(100).IsRequired();
            i.HasIndex(x => x.ProcessadaEm); // apoio à limpeza por idade
        });
    }
}
