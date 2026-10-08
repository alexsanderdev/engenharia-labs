using F5M06.Pagamentos.Gateway;
using Microsoft.Extensions.Logging;

namespace F5M06.Pagamentos.Checkout;

/// <summary>Resultado do checkout para a API do OrderFlow.</summary>
public sealed record ResultadoCheckout(SituacaoPagamento Situacao, string Mensagem);

/// <summary>Caso de uso "pagar pedido". Não conhece HTTP: só <see cref="IGatewayPagamento"/> e resultados de domínio.</summary>
public sealed partial class CheckoutService(IGatewayPagamento gateway, ILogger<CheckoutService> logger)
{
    /// <summary>
    /// Chave de idempotência DETERMINÍSTICA: <c>pedido-{Id:N}-pagamento-{TentativaDePagamento}</c>.
    /// O mesmo pedido na mesma tentativa sempre gera a mesma chave (clique duplo, retry do pipeline,
    /// reprocessamento por job), então o gateway cobra no máximo uma vez.
    /// </summary>
    public static string ChaveDeIdempotencia(Pedido pedido) =>
        throw new NotImplementedException("TODO (Passo 9): $\"pedido-{pedido.Id:N}-pagamento-{pedido.TentativaDePagamento}\".");

    /// <summary>
    /// Cobra o pedido e atualiza a situação:
    /// Aprovada → <c>ConfirmarPagamento</c> (Pago); Recusada → <c>RegistrarRecusa</c> (Recusado, mensagem = motivo);
    /// Indisponivel → <c>MarcarPagamentoPendente</c> (não é erro do cliente: reconciliar depois);
    /// Rejeitada → <c>MarcarErroDeIntegracao</c> + log de erro. Nunca lança por falha do gateway.
    /// </summary>
    public Task<ResultadoCheckout> PagarAsync(Pedido pedido, string tokenCartao, CancellationToken ct = default)
    {
        _ = (gateway, logger);
        throw new NotImplementedException("TODO (Passo 9): chame gateway.CobrarAsync com a chave do pedido e faça switch no ResultadoCobranca.");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Gateway rejeitou a cobrança do pedido {PedidoId}: {Motivo}")]
    private static partial void LogRejeicao(ILogger logger, Guid pedidoId, MotivoRejeicao motivo);
}
