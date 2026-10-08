using Microsoft.Extensions.Options;

namespace F6M06.Worker.Notificacoes;

/// <summary>
/// Processa a <see cref="FilaDeNotificacoes"/> com N leitores em paralelo e desligamento gracioso:
/// ao parar, deixa de aceitar itens, termina o que está em andamento, não começa itens novos e só
/// interrompe o trabalho em andamento se o <c>HostOptions.ShutdownTimeout</c> estourar.
/// </summary>
public sealed partial class ProcessadorDeNotificacoesWorker(
    FilaDeNotificacoes fila,
    IEnviadorDeNotificacoes enviador,
    IOptions<NotificacoesOptions> opcoes,
    ILogger<ProcessadorDeNotificacoesWorker> logger) : BackgroundService
{
    // Cancelado SÓ quando o prazo de desligamento (ShutdownTimeout) acaba: interrompe o envio em andamento.
    private readonly CancellationTokenSource _abortar = new();

    /// <summary>
    /// <list type="number">
    /// <item>registre no <paramref name="stoppingToken"/> o <see cref="FilaDeNotificacoes.Encerrar"/> (parou → não aceita novos);</item>
    /// <item>dispare <see cref="NotificacoesOptions.Leitores"/> chamadas de <see cref="LerAsync"/> e espere todas (<c>Task.WhenAll</c>);</item>
    /// <item>no fim, se sobrou algo na fila, <see cref="LogPendentesNaoProcessadas"/> (Warning): numa fila em memória
    /// esses itens SE PERDEM — é exatamente por isso que existe broker com ack (6.01) e outbox (6.04).</item>
    /// </list>
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var registro = stoppingToken.Register(fila.Encerrar);

        var leitores = Enumerable.Range(1, opcoes.Value.Leitores).Select(n => LerAsync(n, stoppingToken));
        await Task.WhenAll(leitores);

        if (fila.Pendentes > 0)
            LogPendentesNaoProcessadas(fila.Pendentes);
    }

    /// <summary>
    /// Um leitor: enquanto o <paramref name="stoppingToken"/> NÃO foi cancelado, espera item
    /// (<c>WaitToReadAsync(stoppingToken)</c>; cancelado ou fila encerrada e vazia → sai), confere de novo o
    /// token (parou enquanto esperava → não pega item novo), faz <c>TryRead</c> e processa com
    /// <see cref="ProcessarAsync"/>. Cancelamento do <paramref name="stoppingToken"/> é saída normal.
    /// </summary>
    private async Task LerAsync(int leitor, CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested
                   && await fila.Leitor.WaitToReadAsync(stoppingToken))
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                if (fila.Leitor.TryRead(out var notificacao))
                    await ProcessarAsync(leitor, notificacao);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // parada normal: não começa itens novos
        }
    }

    /// <summary>
    /// Envia UMA notificação usando o token <c>_abortar.Token</c> (NÃO o stoppingToken: parar o host não
    /// interrompe o item em andamento; só o fim do prazo de desligamento interrompe).
    /// Sucesso → <see cref="LogEnviada"/>. Cancelada pelo <c>_abortar</c> → <see cref="LogInterrompida"/> (Warning).
    /// Qualquer outra exceção → <see cref="LogFalhaNoEnvio"/> (Error) e o leitor SEGUE para o próximo item
    /// (uma notificação ruim não pode matar o leitor).
    /// </summary>
    private async Task ProcessarAsync(int leitor, Notificacao notificacao)
    {
        try
        {
            await enviador.EnviarAsync(notificacao, _abortar.Token);
            LogEnviada(notificacao.Id, leitor);
        }
        catch (OperationCanceledException) when (_abortar.IsCancellationRequested)
        {
            LogInterrompida(notificacao.Id);
        }
#pragma warning disable CA1031 // um item com falha não pode derrubar o leitor
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogFalhaNoEnvio(ex, notificacao.Id);
        }
    }

    /// <summary>
    /// O host chama com um <paramref name="cancellationToken"/> que é cancelado quando o
    /// <c>HostOptions.ShutdownTimeout</c> acaba. Chame <c>base.StopAsync(cancellationToken)</c> (que cancela o
    /// stoppingToken e espera o <c>ExecuteAsync</c> terminar OU o prazo acabar). Se ele voltou porque o prazo
    /// acabou (<c>cancellationToken.IsCancellationRequested</c>), cancele <c>_abortar</c>: o item em andamento
    /// é interrompido em vez de continuar rodando "solto" enquanto o processo morre.
    /// </summary>
    /// <remarks>
    /// Pegadinha: registrar <c>cancellationToken.Register(_abortar.Cancel)</c> com <c>using</c> NÃO funciona.
    /// Os callbacks de um token rodam em ordem inversa de registro; o da espera do <c>base.StopAsync</c> roda
    /// primeiro, o método retorna, o <c>using</c> desfaz o seu registro… e o seu callback nunca roda.
    /// </remarks>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            await _abortar.CancelAsync();
    }

    /// <summary>(PRONTO)</summary>
    public override void Dispose()
    {
        _abortar.Dispose();
        base.Dispose();
    }

    [LoggerMessage(EventId = 6101, Level = LogLevel.Information, Message = "Notificação {NotificacaoId} enviada pelo leitor {Leitor}")]
    private partial void LogEnviada(Guid notificacaoId, int leitor);

    [LoggerMessage(EventId = 6102, Level = LogLevel.Warning,
        Message = "Notificação {NotificacaoId} interrompida: o prazo de desligamento acabou")]
    private partial void LogInterrompida(Guid notificacaoId);

    [LoggerMessage(EventId = 6103, Level = LogLevel.Error, Message = "Falha ao enviar a notificação {NotificacaoId}")]
    private partial void LogFalhaNoEnvio(Exception ex, Guid notificacaoId);

    [LoggerMessage(EventId = 6104, Level = LogLevel.Warning,
        Message = "{Quantidade} notificações ficaram na fila em memória e não foram processadas")]
    private partial void LogPendentesNaoProcessadas(int quantidade);
}
