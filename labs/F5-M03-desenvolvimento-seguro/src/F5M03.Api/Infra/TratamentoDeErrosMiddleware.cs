namespace F5M03.Api.Infra;

/// <summary>
/// Tratamento global de exceções.
/// VULNERÁVEL — corrija (Passo 4 do Lab).
/// </summary>
public sealed class TratamentoDeErrosMiddleware(RequestDelegate next, ILogger<TratamentoDeErrosMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // VULNERÁVEL: "ajuda a depurar" — e entrega ao atacante stack trace, tipo da exceção,
            // nome do servidor e o SQL. E nada vai para o log.
            // TODO: logue a exceção completa (LogError) e devolva 500 com ProblemDetails GENÉRICO via
            //       problemDetails.WriteAsync(...) (application/problem+json, com traceId, sem ex.Message).
            //       Só trate se a resposta ainda não começou (context.Response.HasStarted).
            _ = logger;
            _ = problemDetails;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(ex.ToString());
        }
    }
}
