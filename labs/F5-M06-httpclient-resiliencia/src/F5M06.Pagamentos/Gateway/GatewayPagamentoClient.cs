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
    public async Task<ResultadoCobranca> CobrarAsync(SolicitacaoCobranca solicitacao, string chaveIdempotencia, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(solicitacao);
        ArgumentException.ThrowIfNullOrWhiteSpace(chaveIdempotencia);

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, "v1/cobrancas")
        {
            Content = JsonContent.Create(new CobrancaRequestDto(
                solicitacao.PedidoId, solicitacao.Valor, solicitacao.Moeda, solicitacao.TokenCartao)),
        };
        requisicao.Headers.Add(ResilienciaGateway.CabecalhoIdempotencia, chaveIdempotencia);

        try
        {
            using var resposta = await http.SendAsync(requisicao, ct);

            switch (resposta.StatusCode)
            {
                case HttpStatusCode.OK or HttpStatusCode.Created:
                    var corpo = await resposta.Content.ReadFromJsonAsync<CobrancaResponseDto>(ct);
                    return string.IsNullOrWhiteSpace(corpo?.TransacaoId)
                        ? new ResultadoCobranca.Indisponivel(MotivoIndisponibilidade.RespostaInvalida)
                        : new ResultadoCobranca.Aprovada(corpo.TransacaoId);

                case HttpStatusCode.PaymentRequired:
                    var erro = await LerErroAsync(resposta, ct);
                    return new ResultadoCobranca.Recusada(erro?.Codigo ?? "recusada");
            }

            var falha = MapearStatusDeFalha(resposta.StatusCode);
            LogFalhaHttp(logger, "cobrança", (int)resposta.StatusCode);
            return falha;
        }
        catch (Exception ex) when (MotivoDaFalha(ex, ct) is { } motivo)
        {
            LogIndisponivel(logger, "cobrança", motivo, ex);
            return new ResultadoCobranca.Indisponivel(motivo);
        }
    }

    /// <summary>
    /// GET <c>v1/cobrancas/{id}</c>. 200 → status do corpo ("aprovada", "pendente", "recusada", "estornada";
    /// outro → Desconhecido), registra em <see cref="UltimoStatusConhecido"/> e devolve Origem Gateway.
    /// 404 → Desconhecido/Gateway. Qualquer outra falha (status não 2xx ou exceção mapeável) → FALLBACK:
    /// último status conhecido (Origem UltimoConhecido) ou Desconhecido (Origem Padrao).
    /// </summary>
    public async Task<ConsultaStatus> ConsultarStatusAsync(string transacaoId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transacaoId);

        try
        {
            using var resposta = await http.GetAsync($"v1/cobrancas/{Uri.EscapeDataString(transacaoId)}", ct);

            if (resposta.StatusCode == HttpStatusCode.NotFound)
            {
                return new ConsultaStatus(transacaoId, StatusPagamento.Desconhecido, OrigemStatus.Gateway);
            }

            if (resposta.IsSuccessStatusCode)
            {
                var corpo = await resposta.Content.ReadFromJsonAsync<CobrancaResponseDto>(ct);
                var status = ConverterStatus(corpo?.Status);
                ultimoStatus.Registrar(transacaoId, status);
                return new ConsultaStatus(transacaoId, status, OrigemStatus.Gateway);
            }

            LogFalhaHttp(logger, "consulta de status", (int)resposta.StatusCode);
        }
        catch (Exception ex) when (MotivoDaFalha(ex, ct) is { } motivo)
        {
            LogIndisponivel(logger, "consulta de status", motivo, ex);
        }

        return ultimoStatus.TentarObter(transacaoId, out var conhecido)
            ? new ConsultaStatus(transacaoId, conhecido, OrigemStatus.UltimoConhecido)
            : new ConsultaStatus(transacaoId, StatusPagamento.Desconhecido, OrigemStatus.Padrao);
    }

    /// <summary>
    /// POST <c>v1/cobrancas/{id}/estornos</c> SEM Idempotency-Key. 2xx → Sucesso; 429 → LimiteDeRequisicoes;
    /// demais → ErroNoGateway; exceções mapeáveis → motivo correspondente.
    /// </summary>
    public async Task<ResultadoEstorno> EstornarAsync(string transacaoId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transacaoId);

        try
        {
            using var resposta = await http.PostAsync($"v1/cobrancas/{Uri.EscapeDataString(transacaoId)}/estornos", content: null, ct);
            if (resposta.IsSuccessStatusCode)
            {
                return new ResultadoEstorno(true);
            }

            LogFalhaHttp(logger, "estorno", (int)resposta.StatusCode);
            return new ResultadoEstorno(false, resposta.StatusCode == HttpStatusCode.TooManyRequests
                ? MotivoIndisponibilidade.LimiteDeRequisicoes
                : MotivoIndisponibilidade.ErroNoGateway);
        }
        catch (Exception ex) when (MotivoDaFalha(ex, ct) is { } motivo)
        {
            LogIndisponivel(logger, "estorno", motivo, ex);
            return new ResultadoEstorno(false, motivo);
        }
    }

    /// <summary>
    /// Traduz exceções da pilha HTTP/Polly: <see cref="TimeoutRejectedException"/> → Timeout;
    /// <see cref="BrokenCircuitException"/> → CircuitoAberto; <see cref="HttpRequestException"/> → FalhaDeRede;
    /// <c>JsonException</c> (corpo fora do contrato) → RespostaInvalida;
    /// <see cref="OperationCanceledException"/> SEM o <paramref name="ct"/> do chamador cancelado → Timeout.
    /// Qualquer outra coisa (inclusive cancelamento pedido pelo chamador) → <c>null</c> (não captura: propaga).
    /// </summary>
    public static MotivoIndisponibilidade? MotivoDaFalha(Exception ex, CancellationToken ct) => ex switch
    {
        TimeoutRejectedException => MotivoIndisponibilidade.Timeout,
        BrokenCircuitException => MotivoIndisponibilidade.CircuitoAberto,
        HttpRequestException => MotivoIndisponibilidade.FalhaDeRede,
        System.Text.Json.JsonException => MotivoIndisponibilidade.RespostaInvalida,
        OperationCanceledException when !ct.IsCancellationRequested => MotivoIndisponibilidade.Timeout,
        _ => null,
    };

    private static ResultadoCobranca MapearStatusDeFalha(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => new ResultadoCobranca.Rejeitada(MotivoRejeicao.DadosInvalidos),
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new ResultadoCobranca.Rejeitada(MotivoRejeicao.NaoAutorizado),
        HttpStatusCode.Conflict => new ResultadoCobranca.Rejeitada(MotivoRejeicao.ConflitoDeIdempotencia),
        HttpStatusCode.TooManyRequests => new ResultadoCobranca.Indisponivel(MotivoIndisponibilidade.LimiteDeRequisicoes),
        >= HttpStatusCode.InternalServerError => new ResultadoCobranca.Indisponivel(MotivoIndisponibilidade.ErroNoGateway),
        _ => new ResultadoCobranca.Rejeitada(MotivoRejeicao.Desconhecido),
    };

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
