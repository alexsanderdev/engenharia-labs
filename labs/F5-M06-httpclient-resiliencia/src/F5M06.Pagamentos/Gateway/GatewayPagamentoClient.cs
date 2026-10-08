using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace F5M06.Pagamentos.Gateway;

/// <summary>
/// Typed client do gateway. Recebe o <see cref="HttpClient"/> pronto do IHttpClientFactory
/// (BaseAddress, handler de API key e pipeline de resiliência já configurados) e traduz
/// status HTTP e exceções em resultados de domínio.
/// </summary>
public sealed partial class GatewayPagamentoClient(
    HttpClient http,
    UltimoStatusConhecido ultimoStatus,
    ILogger<GatewayPagamentoClient> logger) : IGatewayPagamento
{
    /// <summary>
    /// POST <c>v1/cobrancas</c> com <c>JsonContent</c> do <see cref="CobrancaRequestDto"/> e header
    /// <see cref="ResilienciaGateway.CabecalhoIdempotencia"/>. Mapeamento:
    /// 200/201 → <see cref="ResultadoCobranca.Aprovada"/> (transacaoId do corpo);
    /// 402 → <see cref="ResultadoCobranca.Recusada"/> (codigo do corpo ou "recusada");
    /// 400/422 → Rejeitada(DadosInvalidos); 401/403 → Rejeitada(NaoAutorizado); 409 → Rejeitada(ConflitoDeIdempotencia);
    /// 429 → Indisponivel(LimiteDeRequisicoes); 5xx → Indisponivel(ErroNoGateway); outros → Rejeitada(Desconhecido).
    /// Exceções: veja <see cref="MotivoDaFalha"/>. Cancelamento do CHAMADOR propaga.
    /// Corpo 2xx ilegível (<c>JsonException</c>) ou sem transacaoId → Indisponivel(RespostaInvalida).
    /// </summary>
    public Task<ResultadoCobranca> CobrarAsync(SolicitacaoCobranca solicitacao, string chaveIdempotencia, CancellationToken ct = default)
    {
        _ = (http, ultimoStatus, logger);
        throw new NotImplementedException(
            "TODO (Passo 4): monte o HttpRequestMessage (JsonContent + Idempotency-Key), envie, mapeie o status e capture as exceções com MotivoDaFalha.");
    }

    /// <summary>
    /// GET <c>v1/cobrancas/{id}</c>. 200 → status do corpo ("aprovada", "pendente", "recusada", "estornada";
    /// outro → Desconhecido), registra em <see cref="UltimoStatusConhecido"/> e devolve Origem Gateway.
    /// 404 → Desconhecido/Gateway. Qualquer outra falha (status não 2xx ou exceção mapeável) → FALLBACK:
    /// último status conhecido (Origem UltimoConhecido) ou Desconhecido (Origem Padrao).
    /// </summary>
    public Task<ConsultaStatus> ConsultarStatusAsync(string transacaoId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO (Passo 8): GET de status com fallback para o último status conhecido.");

    /// <summary>
    /// POST <c>v1/cobrancas/{id}/estornos</c> SEM Idempotency-Key. 2xx → Sucesso; 429 → LimiteDeRequisicoes;
    /// demais → ErroNoGateway; exceções mapeáveis → motivo correspondente.
    /// </summary>
    public Task<ResultadoEstorno> EstornarAsync(string transacaoId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO (Passo 4): POST de estorno sem chave; o pipeline não deve repeti-lo.");

    /// <summary>
    /// Traduz exceções da pilha HTTP/Polly: <see cref="TimeoutRejectedException"/> → Timeout;
    /// <see cref="BrokenCircuitException"/> → CircuitoAberto; <see cref="HttpRequestException"/> → FalhaDeRede;
    /// <c>JsonException</c> (corpo fora do contrato) → RespostaInvalida;
    /// <see cref="OperationCanceledException"/> SEM o <paramref name="ct"/> do chamador cancelado → Timeout.
    /// Qualquer outra coisa (inclusive cancelamento pedido pelo chamador) → <c>null</c> (não captura: propaga).
    /// </summary>
    public static MotivoIndisponibilidade? MotivoDaFalha(Exception ex, CancellationToken ct) =>
        throw new NotImplementedException("TODO (Passo 4): switch por tipo de exceção; cancelamento do chamador devolve null.");

    // ----- Prontos: use à vontade -----

    private static StatusPagamento ConverterStatus(string? status) => status?.ToUpperInvariant() switch
    {
        "APROVADA" => StatusPagamento.Aprovado,
        "PENDENTE" => StatusPagamento.Pendente,
        "RECUSADA" => StatusPagamento.Recusado,
        "ESTORNADA" => StatusPagamento.Estornado,
        _ => StatusPagamento.Desconhecido,
    };

    private static async Task<ErroGatewayDto?> LerErroAsync(HttpResponseMessage resposta, CancellationToken ct)
    {
        try
        {
            return await resposta.Content.ReadFromJsonAsync<ErroGatewayDto>(ct);
        }
        catch (System.Text.Json.JsonException)
        {
            return null; // corpo de erro fora do contrato não pode derrubar o mapeamento
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway de pagamento respondeu {Status} na {Operacao}")]
    private static partial void LogFalhaHttp(ILogger logger, string operacao, int status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Gateway de pagamento indisponível na {Operacao}: {Motivo}")]
    private static partial void LogIndisponivel(ILogger logger, string operacao, MotivoIndisponibilidade motivo, Exception ex);
}
