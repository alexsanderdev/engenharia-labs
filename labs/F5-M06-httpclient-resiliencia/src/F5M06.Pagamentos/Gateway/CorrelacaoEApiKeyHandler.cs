using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace F5M06.Pagamentos.Gateway;

/// <summary>
/// DelegatingHandler que carimba toda requisição ao gateway com a API key (das options) e um
/// <c>X-Correlation-Id</c>. Registrado ANTES do resilience handler, roda uma vez por chamada lógica:
/// todas as tentativas de um retry carregam o mesmo correlation id.
/// </summary>
public sealed class CorrelacaoEApiKeyHandler(IOptionsMonitor<GatewayPagamentoOptions> options) : DelegatingHandler
{
    public const string CabecalhoApiKey = "X-Api-Key";
    public const string CabecalhoCorrelacao = "X-Correlation-Id";

    /// <summary>
    /// Define <see cref="CabecalhoApiKey"/> com <c>options.CurrentValue.ApiKey</c> (substituindo se já existir)
    /// e, se a requisição ainda não tiver <see cref="CabecalhoCorrelacao"/>, usa o TraceId da
    /// <see cref="Activity.Current"/> ou um GUID novo ("N").
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _ = options;
        throw new NotImplementedException(
            "TODO (Passo 3): adicione X-Api-Key (options.CurrentValue.ApiKey) e X-Correlation-Id (se ausente) e chame base.SendAsync.");
    }
}
