namespace F5M06.Pagamentos.Gateway;

/// <summary>Porta do OrderFlow para o gateway de pagamento. A implementação é um typed client.</summary>
public interface IGatewayPagamento
{
    /// <summary>
    /// Cobra o valor. A <paramref name="chaveIdempotencia"/> vai no header <c>Idempotency-Key</c> e é
    /// o que torna seguro repetir o POST: o gateway devolve a mesma resposta para a mesma chave.
    /// </summary>
    Task<ResultadoCobranca> CobrarAsync(SolicitacaoCobranca solicitacao, string chaveIdempotencia, CancellationToken ct = default);

    /// <summary>Consulta o status. Com o gateway fora, cai no fallback (último status conhecido).</summary>
    Task<ConsultaStatus> ConsultarStatusAsync(string transacaoId, CancellationToken ct = default);

    /// <summary>Estorna. POST SEM chave de idempotência: o pipeline NÃO pode repeti-lo.</summary>
    Task<ResultadoEstorno> EstornarAsync(string transacaoId, CancellationToken ct = default);
}
