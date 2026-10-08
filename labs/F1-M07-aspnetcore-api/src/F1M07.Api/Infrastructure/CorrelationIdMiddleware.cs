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
        var correlationId = context.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.TraceIdentifier = correlationId;

        // OnStarting: o header é escrito imediatamente antes de a resposta começar,
        // mesmo que outro middleware mais adiante limpe/reescreva a resposta (ex.: exception handler).
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
