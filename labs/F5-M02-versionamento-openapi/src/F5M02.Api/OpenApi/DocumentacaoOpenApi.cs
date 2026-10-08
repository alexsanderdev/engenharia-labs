using Scalar.AspNetCore;

namespace F5M02.Api.OpenApi;

/// <summary>
/// Documentos OpenAPI por versão (code-first, gerados em runtime pelo Microsoft.AspNetCore.OpenApi)
/// e a UI do Scalar só em Development.
/// </summary>
public static class DocumentacaoOpenApi
{
    /// <summary>Nomes dos documentos = GroupName do Asp.Versioning ("v1", "v2"). Rotas: /openapi/v1.json e /openapi/v2.json.</summary>
    public static readonly IReadOnlyList<string> Documentos = ["v1", "v2"];

    public static IServiceCollection AddDocumentacaoOpenApi(this IServiceCollection services)
    {
        foreach (var documento in Documentos)
        {
            // Por padrão, cada documento inclui só os endpoints cujo GroupName é o nome do documento —
            // e quem define o GroupName de cada endpoint versionado é o AddApiExplorer do Asp.Versioning.
            services.AddOpenApi(documento, options =>
            {
                options.AddDocumentTransformer<InfoDocumentTransformer>();
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
                options.AddOperationTransformer<DeprecatedOperationTransformer>();
                options.AddSchemaTransformer<ExemplosSchemaTransformer>();
            });
        }
        return services;
    }

    public static WebApplication MapDocumentacaoOpenApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // O documento é o CONTRATO: fica disponível em todos os ambientes (clientes geram código a partir dele).
        app.MapOpenApi();

        // A UI interativa é ferramenta de desenvolvimento: só em Development.
        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference(options => options
                .WithTitle(InfoDocumentTransformer.Titulo)
                .AddDocuments(Documentos));
        }
        return app;
    }
}
