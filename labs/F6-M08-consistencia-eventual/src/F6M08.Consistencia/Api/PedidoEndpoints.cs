using F6M08.Consistencia.Consistencia;
using F6M08.Consistencia.Mensageria;
using Microsoft.AspNetCore.Http.HttpResults;

namespace F6M08.Consistencia.Api;

/// <summary>
/// A borda assíncrona:
/// <list type="number">
/// <item><c>POST /pedidos</c> → <b>202 Accepted</b> + <c>Location: /operacoes/{id}</c> + <c>Retry-After</c>.</item>
/// <item><c>GET /operacoes/{id}</c> → status da operação; quando concluída, traz o token de consistência.</item>
/// <item><c>GET /clientes/{clienteId}/resumo</c> → lê a projeção; com <c>X-Consistency-Token</c>, read-your-writes.</item>
/// </list>
/// </summary>
public static class PedidoEndpoints
{
    public static IEndpointRouteBuilder MapPedidos(this IEndpointRouteBuilder app)
    {
        app.MapPost("/pedidos", CriarPedido);
        app.MapGet("/operacoes/{operacaoId:guid}", ObterOperacao);
        app.MapGet("/clientes/{clienteId:guid}/resumo", ObterResumoAsync);
        return app;
    }

    /// <summary>
    /// Valida (<c>Total &lt;= 0</c> → 400 ProblemDetails), registra a operação, enfileira o comando e devolve
    /// 202 com <c>Location: /operacoes/{id}</c>, header <c>Retry-After: 1</c> e corpo
    /// <see cref="RespostaDaOperacao"/> com status <c>"Processando"</c>.
    /// </summary>
    public static Results<Accepted<RespostaDaOperacao>, ProblemHttpResult> CriarPedido(
        CriarPedidoRequest requisicao,
        RegistroDeOperacoes registro,
        FilaComAtraso<ComandoCriarPedido> fila,
        HttpContext http)
    {
        // TODO (Passo 5): Total <= 0 → TypedResults.Problem(..., statusCode: 400).
        // Guid novo → registro.Iniciar(...) → fila.Publicar(new ComandoCriarPedido(...)) → Retry-After: 1 →
        // TypedResults.Accepted($"/operacoes/{id}", new RespostaDaOperacao(id, "Processando")).
        _ = (requisicao, registro, fila, http);
        throw new NotImplementedException("TODO: Passo 5 — valide, registre a operação, enfileire o comando e devolva 202 + Location + Retry-After.");
    }

    /// <summary>
    /// 404 se a operação não existe. Senão 200 com <see cref="RespostaDaOperacao"/>:
    /// processando → também <c>Retry-After: 1</c>; concluída → <c>PedidoId</c>, <c>Versao</c>,
    /// <c>TokenDeConsistencia</c> e <c>Recurso</c>; falhou → <c>Erro</c>.
    /// </summary>
    public static Results<Ok<RespostaDaOperacao>, NotFound> ObterOperacao(Guid operacaoId, RegistroDeOperacoes registro, HttpContext http)
    {
        // TODO (Passo 5): concluída → TokenDeConsistencia = new TokenDeConsistencia(pedidoId, versao).ToString();
        // Recurso = $"/clientes/{clienteId}/resumo".
        _ = (operacaoId, registro, http);
        throw new NotImplementedException("TODO: Passo 5 — devolva o status da operação (404, Processando com Retry-After, Concluida com token, Falhou).");
    }

    /// <summary>
    /// Lê o resumo pelo <see cref="ServicoDeConsulta"/>. Header <c>X-Consistency-Token</c> presente e inválido → 400
    /// ProblemDetails. Sempre devolve o header <c>X-Read-Source</c> (<c>projecao</c> ou <c>fonte</c>).
    /// </summary>
    public static Task<Results<Ok<Projecao.ResumoDoCliente>, ProblemHttpResult>> ObterResumoAsync(
        Guid clienteId,
        ServicoDeConsulta consulta,
        HttpContext http,
        CancellationToken ct)
    {
        // TODO (Passo 5): header ausente → token null; presente e inválido → 400 ProblemDetails (TokenDeConsistencia.TryParse).
        // Depois: await consulta.ObterResumoAsync(...) e header X-Read-Source = "projecao" ou "fonte" (Cabecalhos.OrigemDaLeitura).
        _ = (clienteId, consulta, http, ct);
        throw new NotImplementedException("TODO: Passo 5 — leia o X-Consistency-Token (400 se inválido), chame o ServicoDeConsulta e devolva X-Read-Source.");
    }
}
