using System.Globalization;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using MP3.Contratos;
using MP3.Notificacoes.Worker.Configuracao;
using MP3.Notificacoes.Worker.Notificacoes;
using MP3.Notificacoes.Worker.Processamento;

namespace MP3.Notificacoes.Worker.Mensageria;

/// <summary>
/// Consumidor de <c>PedidoCriado</c> com <see cref="ServiceBusProcessor"/> em peek-lock e SEM auto-complete:
/// cada mensagem termina com exatamente uma decisão explícita.
/// <list type="table">
/// <item><term>sucesso / duplicata</term><description>Complete.</description></item>
/// <item><term>corpo ilegível</term><description>DeadLetter "ContratoInvalido" (repetir não conserta).</description></item>
/// <item><term><see cref="FalhaPermanenteException"/></term><description>DeadLetter "FalhaPermanente".</description></item>
/// <item><term>outra exceção (transitória)</term><description>Agenda uma cópia com backoff (propriedade <c>mp3-tentativa</c>)
/// e completa a original; esgotou → DeadLetter "TentativasEsgotadas".</description></item>
/// <item><term>desligamento no meio</term><description>Abandon: a mensagem volta para a fila na hora.</description></item>
/// </list>
/// Se o processo morrer sem liquidar, o lock expira e o broker reentrega (DeliveryCount++); acima do
/// MaxDeliveryCount da fila, o próprio broker manda para a DLQ ("MaxDeliveryCountExceeded").
/// </summary>
public sealed partial class ConsumidorDeNotificacoes(
    ServiceBusClient cliente,
    ProcessadorDePedidoCriado processador,
    PoliticaDeRetry politica,
    IEnumerable<IObservadorDeProcessamento> observadores,
    IOptions<ServiceBusOptions> opcoes,
    TimeProvider relogio,
    ILogger<ConsumidorDeNotificacoes> logger) : IHostedService, IAsyncDisposable
{
    public const string PropriedadeTentativa = "mp3-tentativa";

    private readonly IObservadorDeProcessamento[] observadores = [.. observadores];
    private ServiceBusProcessor? processor;
    private ServiceBusSender? sender;
    private long ultimoErroTicks;

    public bool EstaProcessando => processor is { IsProcessing: true, IsClosed: false };

    /// <summary>Quando o processor reportou o último erro (conexão, lock perdido...). Usado pelo health check.</summary>
    public DateTimeOffset? UltimoErroEm
    {
        get
        {
            var ticks = Interlocked.Read(ref ultimoErroTicks);
            return ticks == 0 ? null : new DateTimeOffset(ticks, TimeSpan.Zero);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var o = opcoes.Value;
        sender = cliente.CreateSender(o.Fila);
        processor = cliente.CreateProcessor(o.Fila, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = o.MaxConcorrencia,
            PrefetchCount = o.Prefetch,
            ReceiveMode = ServiceBusReceiveMode.PeekLock,
        });
        processor.ProcessMessageAsync += AoReceberAsync;
        processor.ProcessErrorAsync += AoFalharAsync;
        await processor.StartProcessingAsync(cancellationToken);
        LogIniciado(logger, o.Fila, o.MaxConcorrencia);
    }

    /// <summary>
    /// Graceful shutdown: para de RECEBER, sinaliza o CancellationToken dos handlers em andamento e espera
    /// todos terminarem (cada um completa ou abandona a sua mensagem). Limite: HostOptions.ShutdownTimeout.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (processor is null) return;
        LogParando(logger);
        await processor.StopProcessingAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (processor is not null) await processor.DisposeAsync();
        if (sender is not null) await sender.DisposeAsync();
    }

    private async Task AoReceberAsync(ProcessMessageEventArgs args)
    {
        var mensagem = args.Message;
        using var atividade = Telemetria.Telemetria.IniciarProcessamento(mensagem, opcoes.Value.Fila);

        PedidoCriado evento;
        try
        {
            evento = ConvencoesDeMensagem.Desserializar(mensagem.Body.ToMemory().Span);
        }
        catch (JsonException ex)
        {
            await MandarParaDlqAsync(args, "ContratoInvalido", ex.Message, atividade);
            return;
        }

        atividade?.SetTag("mp3.pedido.id", evento.PedidoId);
        atividade?.SetTag("mp3.evento.id", evento.EventoId);

        try
        {
            var resultado = await processador.ProcessarAsync(evento, args.CancellationToken);
            await args.CompleteMessageAsync(mensagem, CancellationToken.None);
            var desfecho = resultado == ResultadoDoProcessamento.Duplicada ? Desfecho.Duplicada : Desfecho.Notificada;
            atividade.Desfecho(desfecho);
            Notificar(mensagem.MessageId, desfecho);
        }
        catch (FalhaPermanenteException ex)
        {
            await MandarParaDlqAsync(args, "FalhaPermanente", ex.Message, atividade);
        }
        catch (Exception) when (args.CancellationToken.IsCancellationRequested)
        {
            // Desligando no meio do envio: devolve a mensagem para a fila agora (sem esperar o lock expirar).
            // A transação da inbox foi desfeita, então quem pegar a mensagem vai processá-la do zero.
            await args.AbandonMessageAsync(mensagem, cancellationToken: CancellationToken.None);
            atividade.Desfecho(Desfecho.Abandonada);
            Notificar(mensagem.MessageId, Desfecho.Abandonada);
        }
        catch (Exception ex)
        {
            await TratarFalhaTransitoriaAsync(args, ex, atividade);
        }
    }

    private async Task TratarFalhaTransitoriaAsync(ProcessMessageEventArgs args, Exception erro, System.Diagnostics.Activity? atividade)
    {
        var mensagem = args.Message;
        var tentativa = TentativaAtual(mensagem);
        if (!politica.DeveTentarDeNovo(tentativa))
        {
            await MandarParaDlqAsync(args, "TentativasEsgotadas",
                $"{tentativa} tentativa(s). Última falha: {erro.GetType().Name}: {erro.Message}", atividade);
            return;
        }

        var atraso = politica.AtrasoApos(tentativa);
        var copia = new ServiceBusMessage(mensagem); // mesmo MessageId, corpo e propriedades
        copia.ApplicationProperties[PropriedadeTentativa] = tentativa + 1;

        // Ordem importa: agenda a cópia ANTES de completar a original. Se cair entre os dois, sobra uma
        // mensagem a mais (a inbox absorve); na ordem inversa, cair no meio PERDERIA a notificação.
        await sender!.ScheduleMessageAsync(copia, relogio.GetUtcNow() + atraso, CancellationToken.None);
        await args.CompleteMessageAsync(mensagem, CancellationToken.None);

        LogReagendada(logger, mensagem.MessageId, tentativa, atraso.TotalMilliseconds, erro.Message);
        atividade.Desfecho(Desfecho.Reagendada, erro.GetType().Name);
        Notificar(mensagem.MessageId, Desfecho.Reagendada, erro.GetType().Name);
    }

    private async Task MandarParaDlqAsync(ProcessMessageEventArgs args, string motivo, string descricao, System.Diagnostics.Activity? atividade)
    {
        var limite = descricao.Length <= 1024 ? descricao : descricao[..1024];
        await args.DeadLetterMessageAsync(args.Message, motivo, limite, CancellationToken.None);
        LogDeadLetter(logger, args.Message.MessageId, motivo, limite);
        atividade.Desfecho(Desfecho.DeadLetter, motivo);
        Notificar(args.Message.MessageId, Desfecho.DeadLetter, motivo);
    }

    private static int TentativaAtual(ServiceBusReceivedMessage mensagem) =>
        mensagem.ApplicationProperties.TryGetValue(PropriedadeTentativa, out var valor)
            ? Convert.ToInt32(valor, CultureInfo.InvariantCulture)
            : 1;

    private void Notificar(string messageId, Desfecho desfecho, string? motivo = null)
    {
        foreach (var o in observadores) o.Registrar(messageId, desfecho, motivo);
    }

    private Task AoFalharAsync(ProcessErrorEventArgs args)
    {
        Interlocked.Exchange(ref ultimoErroTicks, relogio.GetUtcNow().UtcTicks);
        LogErroDoProcessor(logger, args.Exception, args.ErrorSource.ToString(), args.EntityPath);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Consumindo a fila {Fila} com concorrência {Concorrencia}.")]
    private static partial void LogIniciado(ILogger logger, string fila, int concorrencia);

    [LoggerMessage(Level = LogLevel.Information, Message = "Parando o consumidor: aguardando as mensagens em andamento.")]
    private static partial void LogParando(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Mensagem {MessageId}: tentativa {Tentativa} falhou ({Erro}); nova tentativa em {AtrasoMs:0} ms.")]
    private static partial void LogReagendada(ILogger logger, string messageId, int tentativa, double atrasoMs, string erro);

    [LoggerMessage(Level = LogLevel.Error, Message = "Mensagem {MessageId} enviada para a DLQ: {Motivo} — {Descricao}")]
    private static partial void LogDeadLetter(ILogger logger, string messageId, string motivo, string descricao);

    [LoggerMessage(Level = LogLevel.Error, Message = "Erro no processor ({Origem}) em {Entidade}.")]
    private static partial void LogErroDoProcessor(ILogger logger, Exception erro, string origem, string entidade);
}
