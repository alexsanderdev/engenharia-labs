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
    public static ServiceBusMessage CriarMensagem(EventoDoPedido evento) =>
        throw new NotImplementedException(
            "TODO (Passo 6): corpo JSON (MensagensDePedido.Json), ContentType, Subject = Tipo, " +
            "SessionId = PedidoId.ToString(\"N\") e MessageId = \"{PedidoId:N}-{Sequencia}\".");

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
#pragma warning disable CS0649 // TODO (Passo 6): o campo é incrementado em ProcessarAsync (apague este pragma).
    private int _ignorados;
#pragma warning restore CS0649

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
    public static ServiceBusSessionProcessorOptions CriarOpcoes(int maxConcurrentSessions) =>
        throw new NotImplementedException(
            "TODO (Passo 6): new ServiceBusSessionProcessorOptions { PeekLock, AutoCompleteMessages = false, " +
            "MaxConcurrentSessions, MaxConcurrentCallsPerSession = 1, SessionIdleTimeout = 2 s }.");

    public Task IniciarAsync(CancellationToken ct = default) => _processor.StartProcessingAsync(ct);

    public Task PararAsync(CancellationToken ct = default) => _processor.StopProcessingAsync(ct);

    private Task ProcessarAsync(ProcessSessionMessageEventArgs args) =>
        // args.GetSessionStateAsync / args.SetSessionStateAsync guardam a última sequência processada da sessão.
        // Para contar um ignorado: Interlocked.Increment(ref _ignorados).
        throw new NotImplementedException(
            "TODO (Passo 6): desserialize o EventoDoPedido; se Sequencia <= última do session state, conte em _ignorados e complete; " +
            "senão chame _handler, grave a nova sequência no session state e complete.");

    public ValueTask DisposeAsync() => _processor.DisposeAsync();
}
