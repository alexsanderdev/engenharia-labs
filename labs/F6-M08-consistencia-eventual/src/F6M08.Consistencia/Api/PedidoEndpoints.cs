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
        if (requisicao.Total <= 0)
            return TypedResults.Problem(title: "Total inválido.", detail: "O total do pedido deve ser maior que zero.",
                statusCode: StatusCodes.Status400BadRequest);

        var operacaoId = Guid.NewGuid();
        registro.Iniciar(operacaoId, requisicao.ClienteId);
        fila.Publicar(new ComandoCriarPedido(operacaoId, requisicao.ClienteId, requisicao.Total));

        http.Response.Headers.RetryAfter = "1";
        return TypedResults.Accepted($"/operacoes/{operacaoId}",
            new RespostaDaOperacao(operacaoId, nameof(StatusDaOperacao.Processando)));
    }

    /// <summary>
    /// 404 se a operação não existe. Senão 200 com <see cref="RespostaDaOperacao"/>:
    /// processando → também <c>Retry-After: 1</c>; concluída → <c>PedidoId</c>, <c>Versao</c>,
    /// <c>TokenDeConsistencia</c> e <c>Recurso</c>; falhou → <c>Erro</c>.
    /// </summary>
    public static Results<Ok<RespostaDaOperacao>, NotFound> ObterOperacao(Guid operacaoId, RegistroDeOperacoes registro, HttpContext http)
    {
        if (registro.Obter(operacaoId) is not { } operacao) return TypedResults.NotFound();

        var resposta = operacao.Status switch
        {
            StatusDaOperacao.Concluida => new RespostaDaOperacao(
                operacao.OperacaoId,
                nameof(StatusDaOperacao.Concluida),
                operacao.PedidoId,
                operacao.Versao,
                new TokenDeConsistencia(operacao.PedidoId!.Value, operacao.Versao!.Value).ToString(),
                $"/clientes/{operacao.ClienteId}/resumo"),
            StatusDaOperacao.Falhou => new RespostaDaOperacao(operacao.OperacaoId, nameof(StatusDaOperacao.Falhou), Erro: operacao.Erro),
            _ => new RespostaDaOperacao(operacao.OperacaoId, nameof(StatusDaOperacao.Processando)),
        };

        if (operacao.Status == StatusDaOperacao.Processando) http.Response.Headers.RetryAfter = "1";
        return TypedResults.Ok(resposta);
    }

    /// <summary>
    /// Lê o resumo pelo <see cref="ServicoDeConsulta"/>. Header <c>X-Consistency-Token</c> presente e inválido → 400
    /// ProblemDetails. Sempre devolve o header <c>X-Read-Source</c> (<c>projecao</c> ou <c>fonte</c>).
    /// </summary>
    public static async Task<Results<Ok<Projecao.ResumoDoCliente>, ProblemHttpResult>> ObterResumoAsync(
        Guid clienteId,
        ServicoDeConsulta consulta,
        HttpContext http,
        CancellationToken ct)
    {
        TokenDeConsistencia? token = null;
        if (http.Request.Headers.TryGetValue(Cabecalhos.TokenDeConsistencia, out var valor))
        {
            if (!TokenDeConsistencia.TryParse(valor.ToString(), out var lido))
                return TypedResults.Problem(title: "Token de consistência inválido.",
                    detail: $"Formato esperado em {Cabecalhos.TokenDeConsistencia}: {{pedidoId sem hífens}}.{{versao}}.",
                    statusCode: StatusCodes.Status400BadRequest);
            token = lido;
        }

        var leitura = await consulta.ObterResumoAsync(clienteId, token, ct);
        http.Response.Headers[Cabecalhos.OrigemDaLeitura] = leitura.Origem == OrigemDaLeitura.Fonte ? "fonte" : "projecao";
        return TypedResults.Ok(leitura.Resumo);
    }
}
