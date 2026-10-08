using F4M07.Patterns.Pagamentos.PagaFacil;

namespace F4M07.Patterns.Pagamentos;

/// <summary>
/// ADAPTER: implementa a porta <see cref="IGatewayDePagamento"/> traduzindo para/de o SDK do PagaFácil.
/// Toda a "estranheza" do gateway morre aqui: se trocarmos de gateway, só esta classe muda.
/// </summary>
public sealed class PagaFacilAdapter(PagaFacilClient cliente) : IGatewayDePagamento
{
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
    public Task<ResultadoDaCobranca> CobrarAsync(Cobranca cobranca, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO: valide o valor, monte o PagaFacilChargeRequest, chame cliente.Charge e traduza a resposta " +
            $"(switch no Status; try/catch de {nameof(PagaFacilTimeoutException)}). Nenhum tipo do PagaFácil sai desta classe. ({cliente.GetType().Name})");
}
