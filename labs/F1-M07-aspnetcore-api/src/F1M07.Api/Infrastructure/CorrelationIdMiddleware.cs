namespace F1M07.Api.Infrastructure;

/// <summary>
/// Middleware de correlation id:
/// - se a requisição traz o header "X-Correlation-Id" (não vazio), reutiliza o valor;
/// - senão, gera um novo (Guid sem hífens, formato "N");
/// - guarda o valor em HttpContext.Items[ItemKey] e em HttpContext.TraceIdentifier;
/// - ecoa o valor no header "X-Correlation-Id" da RESPOSTA (inclusive em 404/500);
/// - abre um logging scope com "CorrelationId" para todos os logs da requisição.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        // TODO: leia/gere o correlation id, guarde em Items e TraceIdentifier,
        //       registre context.Response.OnStarting(...) para escrever o header na resposta
        //       e envolva a chamada abaixo em logger.BeginScope(...).
        _ = logger;
        await next(context);
    }
}
