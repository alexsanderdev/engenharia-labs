using System.Security.Cryptography;
using F5M05.Api.Autenticacao;
using Microsoft.Extensions.Options;

namespace F5M05.Api.Idempotencia;

/// <summary>
/// Idempotency-Key para endpoints marcados com <see cref="ExigeIdempotenciaAttribute"/>:
/// <list type="bullet">
/// <item>sem chave (ou chave inválida) → 400;</item>
/// <item>mesma chave + mesma requisição já concluída → replay da resposta guardada (+ <c>Idempotent-Replayed: true</c>);</item>
/// <item>mesma chave + requisição diferente → 422;</item>
/// <item>mesma chave em processamento → 409 + <c>Retry-After</c> (o cliente tenta de novo e recebe o replay);</item>
/// <item>resposta 5xx ou exceção → libera a chave (retry seguro); 2xx/4xx → guarda para replay.</item>
/// </list>
/// A chave é escopada por cliente: a chave "abc" da Ana não devolve o pedido do Bruno.
/// </summary>
public sealed class IdempotenciaMiddleware(RequestDelegate next, IArmazemDeIdempotencia armazem, IOptions<IdempotenciaOptions> opcoes)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        if (contexto.GetEndpoint()?.Metadata.GetMetadata<ExigeIdempotenciaAttribute>() is null)
        {
            await next(contexto);
            return;
        }

        var ct = contexto.RequestAborted;
        var chaveRecebida = contexto.Request.Headers[IdempotenciaOptions.Cabecalho].ToString();
        if (string.IsNullOrWhiteSpace(chaveRecebida) || chaveRecebida.Length > opcoes.Value.TamanhoMaximoDaChave)
        {
            await EscreverProblemaAsync(contexto, StatusCodes.Status400BadRequest, "Idempotency-Key inválido",
                $"Envie o header {IdempotenciaOptions.Cabecalho} com 1 a {opcoes.Value.TamanhoMaximoDaChave} caracteres (ex.: um UUID).");
            return;
        }

        var chave = $"{contexto.User.ClienteId() ?? "anonimo"}:{chaveRecebida}";
        var hash = await CalcularHashAsync(contexto.Request, ct);

        var reserva = await armazem.TentarReservarAsync(chave, hash, ct);
        switch (reserva.Situacao)
        {
            case SituacaoDaReserva.Concluida:
                await ReproduzirAsync(contexto, reserva.Resposta!, ct);
                return;

            case SituacaoDaReserva.CorpoDiferente:
                await EscreverProblemaAsync(contexto, StatusCodes.Status422UnprocessableEntity, "Idempotency-Key reutilizado",
                    "Esta chave já foi usada com outra requisição. Gere uma chave nova para uma operação nova.");
                return;

            case SituacaoDaReserva.EmAndamento:
                contexto.Response.Headers.RetryAfter = "1";
                await EscreverProblemaAsync(contexto, StatusCodes.Status409Conflict, "Requisição em andamento",
                    "Uma requisição com esta chave ainda está sendo processada. Tente de novo em instantes.");
                return;
        }

        // Reservada: processa, capturando o corpo da resposta para poder guardá-lo.
        var corpoOriginal = contexto.Response.Body;
        using var buffer = new MemoryStream();
        contexto.Response.Body = buffer;
        try
        {
            await next(contexto);
        }
        catch
        {
            await armazem.LiberarAsync(chave, CancellationToken.None);
            throw;
        }
        finally
        {
            contexto.Response.Body = corpoOriginal;
        }

        var bytes = buffer.ToArray();
        if (contexto.Response.StatusCode >= 500)
        {
            await armazem.LiberarAsync(chave, CancellationToken.None);
        }
        else
        {
            await armazem.ConcluirAsync(chave, new RespostaArmazenada(
                contexto.Response.StatusCode,
                contexto.Response.ContentType,
                contexto.Response.Headers.Location.ToString() is { Length: > 0 } location ? location : null,
                bytes), CancellationToken.None);
        }

        await corpoOriginal.WriteAsync(bytes, ct);
    }

    /// <summary>SHA-256 de método + caminho + corpo: a "impressão digital" da requisição.</summary>
    private static async Task<string> CalcularHashAsync(HttpRequest request, CancellationToken ct)
    {
        request.EnableBuffering(); // permite ler o corpo aqui e de novo no endpoint
        using var conteudo = new MemoryStream();
        await request.Body.CopyToAsync(conteudo, ct);
        request.Body.Position = 0;

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(System.Text.Encoding.UTF8.GetBytes($"{request.Method} {request.Path}\n"));
        sha.AppendData(conteudo.GetBuffer(), 0, (int)conteudo.Length);
        return Convert.ToHexString(sha.GetHashAndReset());
    }

    private static async Task ReproduzirAsync(HttpContext contexto, RespostaArmazenada resposta, CancellationToken ct)
    {
        contexto.Response.StatusCode = resposta.StatusCode;
        if (resposta.ContentType is not null) contexto.Response.ContentType = resposta.ContentType;
        if (resposta.Location is not null) contexto.Response.Headers.Location = resposta.Location;
        contexto.Response.Headers[IdempotenciaOptions.CabecalhoDeReplay] = "true";
        await contexto.Response.Body.WriteAsync(resposta.Corpo, ct);
    }

    private static async Task EscreverProblemaAsync(HttpContext contexto, int status, string titulo, string detalhe)
    {
        contexto.Response.StatusCode = status;
        var problemDetails = contexto.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetails.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = { Status = status, Title = titulo, Detail = detalhe },
        });
    }
}
