namespace F5M03.Api.Infra;

/// <summary>
/// Exceção não tratada → 500 com ProblemDetails GENÉRICO (sem mensagem, tipo, stack trace ou SQL)
/// e com <c>traceId</c> para correlacionar com o log. O detalhe completo vai só para o log do servidor.
/// </summary>
public sealed class TratamentoDeErrosMiddleware(RequestDelegate next, ILogger<TratamentoDeErrosMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted && !context.RequestAborted.IsCancellationRequested)
        {
            logger.LogError(ex, "Erro não tratado em {Metodo} {Caminho}", context.Request.Method, context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await problemDetails.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = ex, // disponível para customização; o writer padrão NÃO serializa a exceção
                ProblemDetails =
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Erro interno",
                    Detail = "Ocorreu um erro inesperado. Informe o traceId ao suporte.",
                },
            });
        }
    }
}
