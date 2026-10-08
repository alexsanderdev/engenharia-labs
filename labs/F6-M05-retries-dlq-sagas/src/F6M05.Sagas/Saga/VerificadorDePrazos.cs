namespace F6M05.Sagas.Saga;

/// <summary>
/// Timeout da saga: encontra sagas esperando o Pagamento além do prazo e entrega a elas um
/// <see cref="PrazoDoPagamentoExpirado"/>, que dispara a compensação.
/// </summary>
/// <remarks>
/// Em produção, isto roda num <c>BackgroundService</c> com <c>PeriodicTimer</c> (módulo 6.06),
/// com o mesmo <see cref="TimeProvider"/>. Alternativas: mensagem agendada (Service Bus
/// <c>ScheduledEnqueueTime</c>), fila com TTL no RabbitMQ. A mensagem de prazo passa pelo mesmo
/// orquestrador que as demais: se o Pagamento responder no mesmo instante, a concorrência otimista
/// decide quem chegou primeiro, e o outro é reavaliado no estado novo.
/// </remarks>
public sealed class VerificadorDePrazos(IRepositorioDeSagas repositorio, OrquestradorSagaPedido orquestrador, TimeProvider tempo)
{
    /// <summary>MessageId determinístico do prazo de uma saga (rodar o verificador duas vezes não duplica nada).</summary>
    public static string MessageIdDoPrazo(Guid pedidoId) => $"{pedidoId:N}:{nameof(PrazoDoPagamentoExpirado)}";

    /// <summary>
    /// Lista as sagas com prazo vencido em <c>tempo.GetUtcNow()</c> e processa um
    /// <see cref="PrazoDoPagamentoExpirado"/> (id de <see cref="MessageIdDoPrazo"/>) para cada uma.
    /// Devolve quantas foram efetivamente compensadas (resultado <see cref="TipoDeResultado.Aplicada"/>).
    /// </summary>
    public Task<int> VerificarAsync(int maximo = 100, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO: liste as sagas vencidas e entregue PrazoDoPagamentoExpirado ao orquestrador (Passo 8).");
}
