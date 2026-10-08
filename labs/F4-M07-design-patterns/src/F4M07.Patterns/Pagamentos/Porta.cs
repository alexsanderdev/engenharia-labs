namespace F4M07.Patterns.Pagamentos;

/// <summary>Pedido de cobrança na linguagem do OrderFlow (reais em <c>decimal</c>, Guid do pedido).</summary>
public sealed record Cobranca(Guid PedidoId, decimal Valor, string TokenDoCartao);

/// <summary>Por que o cartão foi recusado — vocabulário do NOSSO domínio, não do gateway.</summary>
public enum MotivoDeRecusa
{
    SaldoInsuficiente,
    CartaoInvalido,
    SuspeitaDeFraude,
    Outro,
}

/// <summary>Resultado de uma cobrança. Recusa e indisponibilidade são resultados esperados, não exceções.</summary>
public abstract record ResultadoDaCobranca
{
    private ResultadoDaCobranca() { }

    public sealed record Aprovada(string CodigoDeAutorizacao) : ResultadoDaCobranca;

    public sealed record Recusada(MotivoDeRecusa Motivo) : ResultadoDaCobranca;

    /// <summary>Gateway fora do ar/timeout: o chamador pode tentar de novo depois.</summary>
    public sealed record GatewayIndisponivel(string Detalhe) : ResultadoDaCobranca;
}

/// <summary>
/// PORTA do domínio para pagamentos. O caso de uso de checkout depende disto — nunca do SDK do gateway.
/// </summary>
public interface IGatewayDePagamento
{
    Task<ResultadoDaCobranca> CobrarAsync(Cobranca cobranca, CancellationToken ct = default);
}
