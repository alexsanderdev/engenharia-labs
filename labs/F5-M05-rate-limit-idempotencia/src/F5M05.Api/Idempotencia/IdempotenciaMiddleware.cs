using Microsoft.Extensions.Options;

namespace F5M05.Api.Idempotencia;

/// <summary>
/// Idempotency-Key para endpoints marcados com <see cref="ExigeIdempotenciaAttribute"/> (Passo 5):
/// <list type="bullet">
/// <item>sem chave, chave em branco ou maior que TamanhoMaximoDaChave → 400 ProblemDetails;</item>
/// <item>mesma chave + mesma requisição já concluída → replay da resposta guardada (status, Content-Type,
/// Location, corpo) + header <c>Idempotent-Replayed: true</c>;</item>
/// <item>mesma chave + requisição diferente → 422;</item>
/// <item>mesma chave ainda em processamento → 409 + <c>Retry-After</c>;</item>
/// <item>resposta 5xx ou exceção → libera a chave; 2xx/4xx → guarda para replay.</item>
/// </list>
/// A chave deve ser escopada por cliente (ex.: "{cliente_id}:{Idempotency-Key}").
/// </summary>
public sealed class IdempotenciaMiddleware(RequestDelegate next, IArmazemDeIdempotencia armazem, IOptions<IdempotenciaOptions> opcoes)
{
    private readonly IArmazemDeIdempotencia _armazem = armazem;
    private readonly IOptions<IdempotenciaOptions> _opcoes = opcoes;

    public async Task InvokeAsync(HttpContext contexto)
    {
        // TODO (Passo 5):
        //  1. Endpoint sem ExigeIdempotenciaAttribute nos metadados → só chama next.
        //  2. Valide o header IdempotenciaOptions.Cabecalho (_opcoes.Value.TamanhoMaximoDaChave).
        //  3. Hash da requisição: SHA-256 de método + caminho + corpo (Request.EnableBuffering(),
        //     leia o corpo e volte Body.Position = 0 para o endpoint conseguir ler de novo).
        //  4. _armazem.TentarReservarAsync(...) e trate cada SituacaoDaReserva.
        //  5. Reservada: troque Response.Body por um MemoryStream, chame next, restaure o Body,
        //     guarde (ConcluirAsync) ou libere (LiberarAsync, também se next lançar) e copie os bytes para o cliente.
        await next(contexto);
    }
}
