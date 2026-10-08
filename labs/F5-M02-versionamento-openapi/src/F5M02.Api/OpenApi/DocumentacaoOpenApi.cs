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
        // TODO (passo 4): para cada nome em Documentos, services.AddOpenApi(nome, options => { ... }) registrando:
        //   AddDocumentTransformer<InfoDocumentTransformer>(), AddDocumentTransformer<BearerSecuritySchemeTransformer>(),
        //   AddOperationTransformer<DeprecatedOperationTransformer>() e AddSchemaTransformer<ExemplosSchemaTransformer>().
        return services;
    }

    public static WebApplication MapDocumentacaoOpenApi(this WebApplication app)
    {
        // TODO (passo 4): app.MapOpenApi() em TODOS os ambientes (o documento é o contrato) e,
        // SÓ em Development, app.MapScalarApiReference(o => o.WithTitle(...).AddDocuments(Documentos)) (using Scalar.AspNetCore).
        return app;
    }
}
