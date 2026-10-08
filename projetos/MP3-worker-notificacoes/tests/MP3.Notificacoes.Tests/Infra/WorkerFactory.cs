using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using MP3.Contratos;
using MP3.Notificacoes.Worker.Mensageria;
using MP3.Notificacoes.Worker.Notificacoes;
using MP3.Notificacoes.Worker.Processamento;

namespace MP3.Notificacoes.Tests.Infra;

/// <summary>
/// O worker de verdade (Program.cs, hosted services, health checks) em memória, apontando para os containers.
/// Só duas coisas mudam: o canal padrão vira o <see cref="NotificadorDeTeste"/> e o backoff fica curto.
/// </summary>
public sealed class WorkerFactory(AmbienteFixture ambiente, NotificadorDeTeste? notificador = null) : WebApplicationFactory<Program>
{
    public NotificadorDeTeste Notificador { get; } = notificador ?? new NotificadorDeTeste();

    public ObservadorDeTeste Observador { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ServiceBus", ambiente.ServiceBus);
        builder.UseSetting("ConnectionStrings:Inbox", ambiente.Inbox);
        builder.UseSetting("Notificacoes:CanalPadrao", NotificadorDeTeste.NomeDoCanal);
        builder.UseSetting("Notificacoes:Retry:MaxTentativas", "4");
        builder.UseSetting("Notificacoes:Retry:AtrasoBase", "00:00:00.200");
        builder.UseSetting("Notificacoes:Retry:Jitter", "0");
        builder.ConfigureTestServices(s =>
        {
            s.AddSingleton<INotificador>(Notificador);
            s.AddSingleton<IObservadorDeProcessamento>(Observador);
        });
    }

    /// <summary>Sobe o host (hosted services incluídos) e devolve a própria fábrica.</summary>
    public WorkerFactory Iniciar()
    {
        _ = Services; // acessar Services força a construção e o StartAsync do host
        return this;
    }

    public async Task<int> LinhasNaInboxAsync(Guid eventoId)
    {
        await using var conexao = new SqlConnection(ambiente.Inbox);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(
            "SELECT COUNT(*) FROM inbox.MensagensProcessadas WHERE Consumidor = @c AND EventoId = @e", conexao);
        comando.Parameters.AddWithValue("@c", ProcessadorDePedidoCriado.NomeDoConsumidor);
        comando.Parameters.AddWithValue("@e", eventoId);
        return (int)(await comando.ExecuteScalarAsync())!;
    }
}

/// <summary>
/// Notificador programável: registra tudo o que "enviou" e, se o teste quiser, falha ou trava
/// conforme a tentativa (1ª, 2ª...) de cada evento.
/// </summary>
public sealed class NotificadorDeTeste : INotificador
{
    public const string NomeDoCanal = "teste";

    private readonly ConcurrentQueue<Notificacao> enviadas = new();
    private readonly ConcurrentDictionary<Guid, int> tentativas = new();

    public string Canal => NomeDoCanal;

    /// <summary>Executado ANTES de "enviar": (notificação, nº da tentativa deste evento, token). Lance para simular falha.</summary>
    public Func<Notificacao, int, CancellationToken, Task>? Comportamento { get; set; }

    public async Task EnviarAsync(Notificacao notificacao, CancellationToken ct)
    {
        var tentativa = tentativas.AddOrUpdate(notificacao.EventoId, 1, (_, n) => n + 1);
        if (Comportamento is not null) await Comportamento(notificacao, tentativa, ct);
        enviadas.Enqueue(notificacao);
    }

    public int EnviadasPara(Guid eventoId) => enviadas.Count(n => n.EventoId == eventoId);

    public int TentativasPara(Guid eventoId) => tentativas.GetValueOrDefault(eventoId);
}

/// <summary>Registra o desfecho de cada mensagem (por MessageId = EventoId).</summary>
public sealed class ObservadorDeTeste : IObservadorDeProcessamento
{
    private readonly ConcurrentQueue<(string MessageId, Desfecho Desfecho, string? Motivo)> registros = new();

    public void Registrar(string messageId, Desfecho desfecho, string? motivo = null) => registros.Enqueue((messageId, desfecho, motivo));

    public IReadOnlyList<Desfecho> DesfechosDe(Guid eventoId) =>
        [.. registros.Where(r => r.MessageId == eventoId.ToString()).Select(r => r.Desfecho)];
}

public static class AmbienteExtensions
{
    public static async Task PublicarAsync(this AmbienteFixture ambiente, params ServiceBusMessage[] mensagens)
    {
        await using var sender = ambiente.Cliente.CreateSender(ConvencoesDeMensagem.FilaDeNotificacoes);
        await sender.SendMessagesAsync(mensagens);
    }

    /// <summary>Procura (sem consumir) a mensagem na DLQ. Devolve null se não estiver lá.</summary>
    public static async Task<ServiceBusReceivedMessage?> NaDlqAsync(this AmbienteFixture ambiente, Guid eventoId)
    {
        await using var receiver = ambiente.Cliente.CreateReceiver(ConvencoesDeMensagem.FilaDeNotificacoes,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });
        var espiadas = await receiver.PeekMessagesAsync(250);
        return espiadas.FirstOrDefault(m => m.MessageId == eventoId.ToString());
    }
}
