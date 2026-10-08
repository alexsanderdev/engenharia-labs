using F5M03.Api.Infra;

namespace F5M03.Api.Seguranca;

/// <summary>
/// Configuração de segurança de borda num lugar só (secure by default):
/// limites do servidor, CORS restrito, HSTS fora de Development, cabeçalhos, erros e logs seguros.
/// </summary>
public static class SegurancaExtensions
{
    public const string SecaoDeOrigens = "Cors:OrigensPermitidas";

    public static WebApplicationBuilder AddSegurancaDeBorda(this WebApplicationBuilder builder)
    {
        // Limites do servidor (valem no Kestrel real; TestServer não usa Kestrel).
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.AddServerHeader = false;
            kestrel.Limits.MaxRequestBodySize = 2 * 1024 * 1024; // teto global; endpoints podem baixar
        });

        // CORS: origens explícitas vindas de configuração. Credenciais só com lista fechada.
        var origens = builder.Configuration.GetSection(SecaoDeOrigens).Get<string[]>() ?? [];
        builder.Services.AddCors(cors => cors.AddDefaultPolicy(politica => politica
            .WithOrigins(origens)
            .WithMethods("GET", "POST")
            .WithHeaders("Content-Type", "Authorization")
            .AllowCredentials()));

        // HSTS: 1 ano. Só é emitido em HTTPS e fora de localhost (UseHsts cuida disso).
        builder.Services.AddHsts(hsts =>
        {
            hsts.MaxAge = TimeSpan.FromDays(365);
            hsts.IncludeSubDomains = true;
        });

        return builder;
    }

    public static WebApplication UseSegurancaDeBorda(this WebApplication app)
    {
        // A ordem importa:
        // 1. Cabeçalhos primeiro (OnStarting): valem para qualquer resposta, inclusive erro.
        app.UseMiddleware<CabecalhosDeSegurancaMiddleware>();
        // 2. Log da requisição (sem segredos), envolvendo tudo que vem depois.
        app.UseMiddleware<LogDeRequisicaoMiddleware>();
        // 3. Erros: converte exceção em ProblemDetails genérico.
        app.UseMiddleware<TratamentoDeErrosMiddleware>();
        // 4. HSTS só fora de Development (em dev, um HSTS em localhost "gruda" no navegador).
        if (!app.Environment.IsDevelopment()) app.UseHsts();
        // 5. CORS antes dos endpoints.
        app.UseCors();
        return app;
    }
}
