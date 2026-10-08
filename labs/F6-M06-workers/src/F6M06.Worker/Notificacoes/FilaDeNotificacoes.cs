using System.ComponentModel.DataAnnotations;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace F6M06.Worker.Notificacoes;

/// <summary>Notificação a enviar ao cliente (e-mail/SMS simulados). (PRONTO)</summary>
public sealed record Notificacao(Guid Id, Guid PedidoId, string Texto);

/// <summary>Quem envia de fato (SMTP, SMS…). Nos testes, um fake controlável. (PRONTO)</summary>
public interface IEnviadorDeNotificacoes
{
    Task EnviarAsync(Notificacao notificacao, CancellationToken ct);
}

/// <summary>Enviador padrão: só loga. (PRONTO)</summary>
public sealed partial class EnviadorDeNotificacoesNoLog(ILogger<EnviadorDeNotificacoesNoLog> logger) : IEnviadorDeNotificacoes
{
    public Task EnviarAsync(Notificacao notificacao, CancellationToken ct)
    {
        LogEnviada(notificacao.PedidoId, notificacao.Texto);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notificação do pedido {PedidoId}: {Texto}")]
    private partial void LogEnviada(Guid pedidoId, string texto);
}

/// <summary>Configuração do processamento de notificações (seção "Notificacoes"). (PRONTO)</summary>
public sealed class NotificacoesOptions
{
    public const string Secao = "Notificacoes";

    /// <summary>Capacidade da fila em memória: cheia, quem produz ESPERA (backpressure).</summary>
    [Range(1, 100_000)]
    public int Capacidade { get; set; } = 100;

    /// <summary>Quantos leitores processam em paralelo.</summary>
    [Range(1, 64)]
    public int Leitores { get; set; } = 2;
}

/// <summary>
/// Fila em memória LIMITADA entre quem produz notificações (ex.: um consumidor de <c>PedidoCriado</c>)
/// e o <see cref="ProcessadorDeNotificacoesWorker"/>. SINGLETON.
/// </summary>
/// <remarks>
/// Fila em memória não é broker: o que estiver nela some se o processo morrer. Serve para desacoplar
/// ritmo e limitar concorrência DENTRO de um processo.
/// </remarks>
public sealed class FilaDeNotificacoes
{
    private readonly Channel<Notificacao> _canal;

    /// <summary>
    /// Crie um <c>Channel.CreateBounded&lt;Notificacao&gt;</c> com <see cref="NotificacoesOptions.Capacidade"/>,
    /// <c>FullMode = BoundedChannelFullMode.Wait</c> (cheio → o produtor espera; nada é descartado),
    /// <c>SingleReader = false</c> e <c>SingleWriter = false</c>.
    /// </summary>
    public FilaDeNotificacoes(IOptions<NotificacoesOptions> opcoes)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        _canal = null!;
        throw new NotImplementedException(
            "TODO (Passo 7): Channel.CreateBounded<Notificacao>(new BoundedChannelOptions(Capacidade) { FullMode = Wait, SingleReader = false, SingleWriter = false })");
    }

    /// <summary>Lado de leitura (para o processador). (PRONTO)</summary>
    public ChannelReader<Notificacao> Leitor => _canal.Reader;

    /// <summary>Itens esperando na fila.</summary>
    public int Pendentes => _canal.Reader.Count;

    /// <summary>
    /// Enfileira esperando vaga se a fila estiver cheia (backpressure: o produtor desacelera).
    /// Depois de <see cref="Encerrar"/>, lança <see cref="ChannelClosedException"/>.
    /// </summary>
    public ValueTask EnfileirarAsync(Notificacao notificacao, CancellationToken ct = default) =>
        _canal.Writer.WriteAsync(notificacao, ct);

    /// <summary>Tenta enfileirar SEM esperar: <c>false</c> se a fila estiver cheia ou encerrada.</summary>
    public bool TentarEnfileirar(Notificacao notificacao) => _canal.Writer.TryWrite(notificacao);

    /// <summary>Para de aceitar itens novos (idempotente). Os que já estão na fila continuam legíveis.</summary>
    public void Encerrar() => _canal.Writer.TryComplete();
}
