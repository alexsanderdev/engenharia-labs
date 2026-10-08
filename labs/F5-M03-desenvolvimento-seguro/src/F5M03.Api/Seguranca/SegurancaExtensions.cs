using F5M03.Api.Infra;

namespace F5M03.Api.Seguranca;

/// <summary>
/// Configuração de segurança de borda num lugar só (secure by default).
/// VULNERÁVEL — corrija (Passos 6, 7 e 8 do Lab).
/// </summary>
public static class SegurancaExtensions
{
    public const string SecaoDeOrigens = "Cors:OrigensPermitidas";

    public static WebApplicationBuilder AddSegurancaDeBorda(this WebApplicationBuilder builder)
    {
        // VULNERÁVEL: "AllowAnyOrigin().AllowCredentials()" o próprio ASP.NET Core recusa, então alguém
        // "resolveu" com SetIsOriginAllowed(_ => true) — que reflete QUALQUER origem com credenciais. Pior ainda.
        // TODO: use as origens de configuração (seção SecaoDeOrigens), só os métodos GET e POST e
        //       só os headers Content-Type e Authorization.
        builder.Services.AddCors(cors => cors.AddDefaultPolicy(politica => politica
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

        // TODO: HSTS com MaxAge de 365 dias (AddHsts).
        // TODO (opcional, só vale no Kestrel real): builder.WebHost.ConfigureKestrel(...) com
        //       AddServerHeader = false e Limits.MaxRequestBodySize.

        return builder;
    }

    public static WebApplication UseSegurancaDeBorda(this WebApplication app)
    {
        // Pense na ORDEM: quem precisa envolver quem?
        app.UseMiddleware<LogDeRequisicaoMiddleware>();
        app.UseMiddleware<TratamentoDeErrosMiddleware>();
        app.UseMiddleware<CabecalhosDeSegurancaMiddleware>();
        // TODO: UseHsts() fora de Development.
        app.UseCors();
        return app;
    }
}
