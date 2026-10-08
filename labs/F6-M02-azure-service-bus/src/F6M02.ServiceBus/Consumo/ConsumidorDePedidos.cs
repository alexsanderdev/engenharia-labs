using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Contratos;
using F6M02.ServiceBus.Publicacao;

namespace F6M02.ServiceBus.Consumo;

/// <summary>Handler de negócio: recebe o evento já desserializado e diz o que aconteceu.</summary>
public delegate Task<ResultadoDoProcessamento> HandlerDePedido(
    PedidoCriado evento, ContextoDaMensagem contexto, CancellationToken ct);

/// <summary>Configuração do consumidor.</summary>
public sealed record ConsumidorDePedidosOptions
{
    /// <summary>Quantas mensagens o processor processa ao mesmo tempo (padrão do SDK: 1).</summary>
    public int MaxConcurrentCalls { get; init; } = 1;

    /// <summary>Por quanto tempo o processor renova o lock sozinho enquanto o handler roda.</summary>
    public TimeSpan MaxAutoLockRenewalDuration { get; init; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Consumidor de <see cref="PedidoCriado"/> sobre <see cref="ServiceBusProcessor"/> em modo
/// <b>peek-lock</b>, com liquidação (settlement) explícita:
/// <list type="bullet">
/// <item>corpo inválido → <c>DeadLetterMessageAsync</c> com motivo <see cref="MotivosDeDeadLetter.MensagemInvalida"/>;</item>
/// <item><see cref="ResultadoDoProcessamento.Sucesso"/> → <c>CompleteMessageAsync</c>;</item>
/// <item><see cref="ResultadoDoProcessamento.FalhaTransitoria"/> → <c>AbandonMessageAsync</c> gravando a propriedade <c>ultimoMotivo</c>;</item>
/// <item><see cref="ResultadoDoProcessamento.FalhaPermanente"/> → <c>DeadLetterMessageAsync(motivo, descrição)</c>;</item>
/// <item>exceção no handler → <c>AbandonMessageAsync</c> (o broker conta a entrega; ao exceder o MaxDeliveryCount, DLQ).</item>
/// </list>
/// </summary>
public sealed class ConsumidorDePedidos : IAsyncDisposable
{
    /// <summary>Propriedade gravada na mensagem ao abandonar (aparece na próxima entrega).</summary>
    public const string PropriedadeUltimoMotivo = "ultimoMotivo";

    private readonly ServiceBusProcessor _processor;
    private readonly HandlerDePedido _handler;
    private readonly ConcurrentQueue<Exception> _erros = new();

    public ConsumidorDePedidos(
        ServiceBusClient cliente,
        OrigemDasMensagens origem,
        HandlerDePedido handler,
        ConsumidorDePedidosOptions? opcoes = null)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        ArgumentNullException.ThrowIfNull(origem);
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));

        _processor = origem.CriarProcessor(cliente, CriarOpcoesDoProcessor(opcoes ?? new()));
        _processor.ProcessMessageAsync += ProcessarAsync;
        _processor.ProcessErrorAsync += args =>
        {
            _erros.Enqueue(args.Exception);
            return Task.CompletedTask;
        };
    }

    /// <summary>Erros reportados pelo processor (conexão, lock perdido, settlement que falhou...).</summary>
    public IReadOnlyCollection<Exception> Erros => _erros;

    /// <summary>
    /// Opções do processor: <c>ReceiveMode = PeekLock</c>, <c>AutoCompleteMessages = false</c>
    /// (quem liquida é o consumidor), <c>MaxConcurrentCalls</c> e <c>MaxAutoLockRenewalDuration</c>
    /// vindos das opções, e <c>PrefetchCount = 0</c> (prefetch com lock curto = locks expirando no buffer).
    /// </summary>
    public static ServiceBusProcessorOptions CriarOpcoesDoProcessor(ConsumidorDePedidosOptions opcoes) =>
        throw new NotImplementedException(
            "TODO (Passo 4): new ServiceBusProcessorOptions { ReceiveMode = PeekLock, AutoCompleteMessages = false, " +
            "MaxConcurrentCalls, MaxAutoLockRenewalDuration, PrefetchCount = 0 }.");

    public Task IniciarAsync(CancellationToken ct = default) => _processor.StartProcessingAsync(ct);

    public Task PararAsync(CancellationToken ct = default) => _processor.StopProcessingAsync(ct);

    private Task ProcessarAsync(ProcessMessageEventArgs args) =>
        // Use args.CompleteMessageAsync, args.AbandonMessageAsync(mensagem, UltimoMotivo(...)) e
        // args.DeadLetterMessageAsync(mensagem, motivo, descrição). Sempre passe args.CancellationToken.
        throw new NotImplementedException(
            "TODO (Passo 4): leia o evento (MensagemInvalidaException → DLQ 'MensagemInvalida'), monte o ContextoDaMensagem, " +
            "chame _handler e liquide conforme o resultado; exceção do handler → Abandon.");

    private static Dictionary<string, object> UltimoMotivo(string motivo) =>
        new() { [PropriedadeUltimoMotivo] = Limitar(motivo) };

    private static string Limitar(string texto) => texto.Length <= 1024 ? texto : texto[..1024];

    public async ValueTask DisposeAsync()
    {
        await _processor.DisposeAsync();
    }
}
