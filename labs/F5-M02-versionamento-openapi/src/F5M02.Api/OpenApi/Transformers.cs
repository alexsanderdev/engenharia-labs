using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace F5M02.Api.OpenApi;

/// <summary>
/// Document transformer: título, versão, descrição e contato do documento. Para uma versão depreciada,
/// a descrição avisa a depreciação e a data de sunset (vindas das políticas do Asp.Versioning).
/// </summary>
public sealed class InfoDocumentTransformer(IApiVersionDescriptionProvider versoes) : IOpenApiDocumentTransformer
{
    public const string Titulo = "OrderFlow API";

    /// <summary>
    /// Ache em <c>versoes.ApiVersionDescriptions</c> a versão cujo <c>GroupName</c> é <c>context.DocumentName</c>.
    /// <c>Info.Title</c> = <see cref="Titulo"/>; <c>Info.Version</c> = <c>ApiVersion.ToString()</c> ("1.0");
    /// <c>Info.Contact</c> à sua escolha; <c>Info.Description</c> cita "RFC 9457" e, se <c>IsDeprecated</c>,
    /// contém "depreciada" e a data de <c>SunsetPolicy.Date</c> no formato yyyy-MM-dd.
    /// </summary>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken) =>
        throw new NotImplementedException("TODO (passo 4): preencha document.Info a partir do IApiVersionDescriptionProvider.");
}

/// <summary>Document transformer: declara o esquema de segurança Bearer (JWT) e o exige no documento todo.</summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string NomeEsquema = "Bearer";

    /// <summary>
    /// <c>document.Components.SecuritySchemes["Bearer"]</c> = <c>OpenApiSecurityScheme</c> (Http, "bearer", "JWT") e
    /// um <c>OpenApiSecurityRequirement</c> em <c>document.Security</c> referenciando-o com
    /// <c>new OpenApiSecuritySchemeReference("Bearer", document)</c>.
    /// </summary>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken) =>
        throw new NotImplementedException("TODO (passo 4): registre o esquema Bearer em Components e o requisito em Security.");
}

/// <summary>Operation transformer: marca como <c>deprecated: true</c> as operações de versões depreciadas.</summary>
public sealed class DeprecatedOperationTransformer : IOpenApiOperationTransformer
{
    /// <summary>Use <c>context.Description.IsDeprecated</c> (extensão do Asp.Versioning.Mvc.ApiExplorer).</summary>
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken) =>
        throw new NotImplementedException("TODO (passo 4): operation.Deprecated = true quando a ApiDescription for de versão depreciada.");
}

/// <summary>Schema transformer: exemplos nos schemas que mais confundem (dinheiro e criação de pedido).</summary>
public sealed class ExemplosSchemaTransformer : IOpenApiSchemaTransformer
{
    /// <summary>
    /// Quando <c>context.JsonTypeInfo.Type</c> for <c>DinheiroResponse</c>, adicione em <c>schema.Examples</c> um
    /// <c>JsonObject</c> { valor: 620.00, moeda: "BRL" }; para <c>CriarPedidoV2Request</c>, um exemplo com clienteId e 1 item.
    /// </summary>
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) =>
        throw new NotImplementedException("TODO (passo 4): adicione exemplos (schema.Examples) para DinheiroResponse e CriarPedidoV2Request.");
}
