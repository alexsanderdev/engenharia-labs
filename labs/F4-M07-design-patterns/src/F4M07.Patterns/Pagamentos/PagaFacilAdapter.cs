using System.Globalization;
using F4M07.Patterns.Pagamentos.PagaFacil;

namespace F4M07.Patterns.Pagamentos;

/// <summary>
/// ADAPTER: implementa a porta <see cref="IGatewayDePagamento"/> traduzindo para/de o SDK do PagaFácil.
/// Toda a "estranheza" do gateway morre aqui: se trocarmos de gateway, só esta classe muda.
/// </summary>
public sealed class PagaFacilAdapter(PagaFacilClient cliente) : IGatewayDePagamento
{
    private const string CodigoBrl = "986";

    /// <summary>
    /// Regras de tradução:
    /// <list type="bullet">
    /// <item>Valor deve ser &gt; 0 (<see cref="ArgumentOutOfRangeException"/>) e ter no máximo 2 casas (<see cref="ArgumentException"/>);
    /// vai como centavos em texto invariável (R$ 19,99 → "1999").</item>
    /// <item>Moeda "986"; <c>MerchantReference</c> = <c>PedidoId.ToString("N")</c> (32 caracteres); <c>CardHash</c> = token.</item>
    /// <item>Status 0 → <see cref="ResultadoDaCobranca.Aprovada"/>; 1 → <see cref="ResultadoDaCobranca.Recusada"/> com o motivo
    /// mapeado ("51", "14", "59", senão <see cref="MotivoDeRecusa.Outro"/>); qualquer outro status ou
    /// <see cref="PagaFacilTimeoutException"/> → <see cref="ResultadoDaCobranca.GatewayIndisponivel"/>.</item>
    /// </list>
    /// </summary>
    public Task<ResultadoDaCobranca> CobrarAsync(Cobranca cobranca, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var requisicao = new PagaFacilChargeRequest
        {
            AmountInCents = ParaCentavos(cobranca.Valor).ToString(CultureInfo.InvariantCulture),
            CurrencyCode = CodigoBrl,
            MerchantReference = cobranca.PedidoId.ToString("N"),
            CardHash = cobranca.TokenDoCartao,
        };

        ResultadoDaCobranca resultado;
        try
        {
            var resposta = cliente.Charge(requisicao);
            resultado = resposta.Status switch
            {
                0 => new ResultadoDaCobranca.Aprovada(resposta.AuthCode ?? string.Empty),
                1 => new ResultadoDaCobranca.Recusada(TraduzirRecusa(resposta.DeclineCode)),
                _ => new ResultadoDaCobranca.GatewayIndisponivel($"PagaFácil devolveu status {resposta.Status}"),
            };
        }
        catch (PagaFacilTimeoutException ex)
        {
            resultado = new ResultadoDaCobranca.GatewayIndisponivel(ex.Message);
        }

        return Task.FromResult(resultado);
    }

    private static long ParaCentavos(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        var centavos = valor * 100m;
        if (centavos != decimal.Truncate(centavos))
            throw new ArgumentException($"Valor {valor} tem mais de 2 casas decimais.", nameof(valor));
        return (long)centavos;
    }

    private static MotivoDeRecusa TraduzirRecusa(string? codigo) => codigo switch
    {
        "51" => MotivoDeRecusa.SaldoInsuficiente,
        "14" => MotivoDeRecusa.CartaoInvalido,
        "59" => MotivoDeRecusa.SuspeitaDeFraude,
        _ => MotivoDeRecusa.Outro,
    };
}
