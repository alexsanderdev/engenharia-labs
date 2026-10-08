using F6M04.Notificacoes;
using F6M04.Notificacoes.Contratos;
using F6M04.Notificacoes.Dominio;
using F6M04.Notificacoes.Inbox;
using F6M04.Notificacoes.Infra;
using F6M04.Notificacoes.Mensageria;
using F6M04.Pedidos;
using F6M04.Pedidos.Aplicacao;
using F6M04.Pedidos.Infra;
using F6M04.Pedidos.Mensageria;
using F6M04.Pedidos.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace F6M04.Tests.Infra;

/// <summary>
/// PRONTA. Base dos testes: limpa os bancos, cria uma topologia EXCLUSIVA do teste no RabbitMQ (exchange e filas
/// com sufixo único), um relógio falso e um contêiner de DI montado com o mesmo <c>AddPedidos</c>/<c>AddNotificacoes</c>
/// que a aplicação usaria.
/// </summary>
public abstract class TesteComInfra(InfraFixture infra) : IAsyncLifetime
{
    private readonly List<ServiceProvider> _provedores = [];
    private BrokerDeTeste? _broker;

    protected InfraFixture Infra { get; } = infra;

    /// <summary>Relógio controlado (backoff, retenção e o PeriodicTimer do processor usam este relógio).</summary>
    protected FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));

    /// <summary>Publicador em memória usado por padrão (os testes de broker real pedem o RabbitMQ explicitamente).</summary>
    protected PublicadorQueGrava Publicador { get; } = new();

    /// <summary>Exchange, fila e DLQ deste teste.</summary>
    protected NotificacoesRabbitMqOptions Topologia { get; private set; } = new();

    protected BrokerDeTeste Broker => _broker ?? throw new InvalidOperationException("Broker não inicializado.");

    /// <summary>Contêiner padrão do teste: Pedidos + Notificações, com o <see cref="Publicador"/> em memória.</summary>
    protected ServiceProvider Servicos { get; private set; } = null!;

    /// <summary>Opções da Outbox nos testes: lote 50, no máximo 3 tentativas, backoff de 10 s (dobrando) até 1 min.</summary>
    protected static void OpcoesDeTeste(OutboxOptions o)
    {
        o.TamanhoDoLote = 50;
        o.MaximoDeTentativas = 3;
        o.EsperaInicial = TimeSpan.FromSeconds(10);
        o.EsperaMaxima = TimeSpan.FromMinutes(1);
        o.Intervalo = TimeSpan.FromSeconds(5);
        o.RetencaoDasProcessadas = TimeSpan.FromDays(7);
    }

    public virtual async ValueTask InitializeAsync()
    {
        await Infra.ResetarBancosAsync();

        var n = Infra.Proximo();
        Topologia = new NotificacoesRabbitMqOptions
        {
            ConnectionString = Infra.AmqpUri,
            Exchange = $"orderflow.pedidos.t{n}",
            Fila = $"notificacoes.pedidos.t{n}",
            FilaDeMensagensMortas = $"notificacoes.pedidos.t{n}.dlq",
            Assinatura = "pedido.*",
            Prefetch = 10,
        };
        _broker = await BrokerDeTeste.ConectarAsync(Infra.AmqpUri, Topologia);

        Servicos = CriarServicos();
    }

    /// <summary>
    /// Monta um contêiner de DI como a aplicação faria. <paramref name="brokerReal"/> = usa o
    /// <see cref="PublicadorRabbitMq"/> registrado por <c>AddPedidos</c>; senão, o <see cref="Publicador"/> em memória.
    /// </summary>
    protected ServiceProvider CriarServicos(
        bool brokerReal = false,
        Action<IServiceCollection>? ajustar = null,
        Action<DbContextOptionsBuilder>? dbPedidos = null,
        Action<DbContextOptionsBuilder>? dbNotificacoes = null)
    {
        var servicos = new ServiceCollection();
        servicos.AddSingleton<TimeProvider>(Relogio);

        servicos.AddPedidos(
            Infra.PedidosCs,
            r =>
            {
                r.ConnectionString = Infra.AmqpUri;
                r.Exchange = Topologia.Exchange;
            },
            OpcoesDeTeste,
            dbPedidos);

        servicos.AddNotificacoes(
            Infra.NotificacoesCs,
            r =>
            {
                r.ConnectionString = Topologia.ConnectionString;
                r.Exchange = Topologia.Exchange;
                r.Fila = Topologia.Fila;
                r.FilaDeMensagensMortas = Topologia.FilaDeMensagensMortas;
                r.Assinatura = Topologia.Assinatura;
                r.Prefetch = Topologia.Prefetch;
            },
            dbNotificacoes);

        if (!brokerReal) servicos.AddSingleton<IPublicadorDeMensagens>(Publicador);
        ajustar?.Invoke(servicos);

        var provedor = servicos.BuildServiceProvider(validateScopes: true);
        _provedores.Add(provedor);
        return provedor;
    }

    /// <summary>Um <see cref="OutboxProcessor"/> "avulso" (outra instância), com o publicador e as opções que o teste quiser.</summary>
    protected OutboxProcessor CriarProcessor(IPublicadorDeMensagens publicador, Action<OutboxOptions>? ajustar = null, IServiceProvider? servicos = null)
    {
        var opcoes = new OutboxOptions();
        OpcoesDeTeste(opcoes);
        ajustar?.Invoke(opcoes);
        return new OutboxProcessor(
            (servicos ?? Servicos).GetRequiredService<IServiceScopeFactory>(),
            publicador,
            Relogio,
            Options.Create(opcoes),
            NullLogger<OutboxProcessor>.Instance);
    }

    /// <summary>Roda rodadas até uma rodada não publicar nada. Devolve o total publicado.</summary>
    protected static async Task<int> ProcessarAteEsvaziarAsync(OutboxProcessor processor)
    {
        var total = 0;
        for (var rodada = 0; rodada < 20; rodada++)
        {
            var publicadas = await processor.ProcessarLoteAsync();
            if (publicadas == 0) return total;
            total += publicadas;
        }
        throw new InvalidOperationException("A Outbox não esvaziou em 20 rodadas.");
    }

    protected async Task<Guid> CriarPedidoAsync(string numero, decimal total = 150m, IServiceProvider? servicos = null)
    {
        await using var escopo = (servicos ?? Servicos).CreateAsyncScope();
        var servico = escopo.ServiceProvider.GetRequiredService<ServicoDePedidos>();
        return await servico.CriarAsync(new CriarPedido(numero, $"{numero.ToLowerInvariant()}@cliente.test", total));
    }

    protected async Task ConfirmarPedidoAsync(Guid pedidoId, IServiceProvider? servicos = null)
    {
        await using var escopo = (servicos ?? Servicos).CreateAsyncScope();
        await escopo.ServiceProvider.GetRequiredService<ServicoDePedidos>().ConfirmarAsync(pedidoId);
    }

    /// <summary>Linhas da Outbox em ordem de gravação (lidas sem cache, direto do banco).</summary>
    protected async Task<List<OutboxMessage>> LerOutboxAsync()
    {
        await using var escopo = Servicos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();
        return await db.OutboxMessages.AsNoTracking().OrderBy(m => m.Sequencia).ToListAsync();
    }

    protected async Task<int> ContarPedidosAsync()
    {
        await using var escopo = Servicos.CreateAsyncScope();
        return await escopo.ServiceProvider.GetRequiredService<PedidosDbContext>().Pedidos.CountAsync();
    }

    protected async Task<List<Notificacao>> LerNotificacoesAsync()
    {
        await using var escopo = Servicos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<NotificacoesDbContext>();
        return await db.Notificacoes.AsNoTracking().OrderBy(n => n.CriadaEm).ThenBy(n => n.Tipo).ToListAsync();
    }

    protected async Task<List<InboxMessage>> LerInboxAsync()
    {
        await using var escopo = Servicos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<NotificacoesDbContext>();
        return await db.InboxMessages.AsNoTracking().ToListAsync();
    }

    /// <summary>Processa uma mensagem com o consumidor idempotente, num escopo próprio (como o worker faz).</summary>
    protected static async Task<ResultadoDoProcessamento> ConsumirAsync(IServiceProvider servicos, MensagemRecebida mensagem)
    {
        await using var escopo = servicos.CreateAsyncScope();
        return await escopo.ServiceProvider.GetRequiredService<ConsumidorDeNotificacoes>().ProcessarAsync(mensagem);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var provedor in _provedores) await provedor.DisposeAsync();
        if (_broker is not null) await _broker.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
