using System.Globalization;
using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Consumo;
using F6M02.ServiceBus.Contratos;
using F6M02.ServiceBus.Publicacao;
using F6M02.ServiceBus.Topologia;

namespace F6M02.ServiceBus.Sessoes;

/// <summary>
/// Publica eventos do ciclo de vida do pedido na fila com sessions <see cref="Entidades.FilaEventosDoPedido"/>.
/// Todas as mensagens de um mesmo pedido vão para a MESMA sessão (<c>SessionId = PedidoId:N</c>),
/// e o broker garante FIFO dentro da sessão e um único consumidor por sessão de cada vez.
/// </summary>
public sealed class PublicadorDeEventosDoPedido(ServiceBusClient cliente) : IAsyncDisposable
{
    private readonly ServiceBusSender _sender = cliente.CreateSender(Entidades.FilaEventosDoPedido);

    /// <summary>
    /// Monta a mensagem do evento: corpo JSON, <c>ContentType = application/json</c>, <c>Subject = Tipo</c>,
    /// <c>SessionId = PedidoId.ToString("N")</c> e <c>MessageId = "{PedidoId:N}-{Sequencia}"</c>.
    /// </summary>
    public static ServiceBusMessage CriarMensagem(EventoDoPedido evento)
    {
        ArgumentNullException.ThrowIfNull(evento);
        return new ServiceBusMessage(BinaryData.FromObjectAsJson(evento, MensagensDePedido.Json))
        {
            SessionId = evento.PedidoId.ToString("N"),
            MessageId = $"{evento.PedidoId:N}-{evento.Sequencia}",
            Subject = evento.Tipo,
            ContentType = MensagensDePedido.ContentTypeJson,
        };
    }

    public Task PublicarAsync(EventoDoPedido evento, CancellationToken ct = default) =>
        _sender.SendMessageAsync(CriarMensagem(evento), ct);

    public ValueTask DisposeAsync() => _sender.DisposeAsync();
}

/// <summary>Handler de negócio para um evento do pedido (já em ordem dentro do pedido).</summary>
public delegate Task HandlerDeEventoDoPedido(EventoDoPedido evento, ContextoDaMensagem contexto, CancellationToken ct);

/// <summary>
/// Processa a fila com sessions usando <see cref="ServiceBusSessionProcessor"/>:
/// <list type="bullet">
/// <item><c>MaxConcurrentSessions</c> = pedidos diferentes em paralelo; <c>MaxConcurrentCallsPerSession = 1</c> = ordem dentro do pedido;</item>
/// <item>peek-lock sem auto-complete: completa depois do handler;</item>
/// <item>guarda no <b>session state</b> a última sequência processada; evento com sequência menor ou igual
/// (reentrega/duplicata) é completado SEM chamar o handler e contado em <see cref="Ignorados"/>.</item>
/// </list>
/// </summary>
public sealed class ProcessadorDeEventosDoPedido : IAsyncDisposable
{
    private readonly ServiceBusSessionProcessor _processor;
    private readonly HandlerDeEventoDoPedido _handler;
    private int _ignorados;

    public ProcessadorDeEventosDoPedido(ServiceBusClient cliente, HandlerDeEventoDoPedido handler, int maxConcurrentSessions = 4)
    {
        ArgumentNullException.ThrowIfNull(cliente);
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));

        _processor = cliente.CreateSessionProcessor(Entidades.FilaEventosDoPedido, CriarOpcoes(maxConcurrentSessions));
        _processor.ProcessMessageAsync += ProcessarAsync;
        _processor.ProcessErrorAsync += _ => Task.CompletedTask;
    }

    /// <summary>Quantos eventos foram descartados por já terem sido processados (sequência repetida/antiga).</summary>
    public int Ignorados => Volatile.Read(ref _ignorados);

    /// <summary>
    /// Opções: PeekLock, <c>AutoCompleteMessages = false</c>, <c>MaxConcurrentSessions</c> informado,
    /// <c>MaxConcurrentCallsPerSession = 1</c> e <c>SessionIdleTimeout</c> curto (2 s) para liberar
    /// sessões vazias e pegar a próxima.
    /// </summary>
    public static ServiceBusSessionProcessorOptions CriarOpcoes(int maxConcurrentSessions) => new()
    {
        ReceiveMode = ServiceBusReceiveMode.PeekLock,
        AutoCompleteMessages = false,
        MaxConcurrentSessions = maxConcurrentSessions,
        MaxConcurrentCallsPerSession = 1,
        SessionIdleTimeout = TimeSpan.FromSeconds(2),
    };

    public Task IniciarAsync(CancellationToken ct = default) => _processor.StartProcessingAsync(ct);

    public Task PararAsync(CancellationToken ct = default) => _processor.StopProcessingAsync(ct);

    private async Task ProcessarAsync(ProcessSessionMessageEventArgs args)
    {
        var ct = args.CancellationToken;
        var mensagem = args.Message;
        var evento = mensagem.Body.ToObjectFromJson<EventoDoPedido>(MensagensDePedido.Json)
            ?? throw new InvalidOperationException("Evento vazio.");

        var estado = await args.GetSessionStateAsync(ct);
        var ultima = estado is null ? 0 : int.Parse(estado.ToString(), CultureInfo.InvariantCulture);

        if (evento.Sequencia <= ultima)
        {
            Interlocked.Increment(ref _ignorados);
            await args.CompleteMessageAsync(mensagem, ct);
            return;
        }

        await _handler(evento,
            new ContextoDaMensagem(mensagem.MessageId, mensagem.CorrelationId, mensagem.DeliveryCount, mensagem.EnqueuedTime, mensagem.SessionId),
            ct);

        // Estado ANTES do complete: se cair entre os dois, a reentrega é reconhecida como repetida.
        await args.SetSessionStateAsync(BinaryData.FromString(evento.Sequencia.ToString(CultureInfo.InvariantCulture)), ct);
        await args.CompleteMessageAsync(mensagem, ct);
    }

    public ValueTask DisposeAsync() => _processor.DisposeAsync();
}
