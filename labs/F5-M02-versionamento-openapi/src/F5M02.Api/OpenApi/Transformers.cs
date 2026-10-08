using System.Globalization;
using System.Text.Json.Nodes;
using Asp.Versioning.ApiExplorer;
using F5M02.Api.Contratos;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
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

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(context);

        var versao = versoes.ApiVersionDescriptions.FirstOrDefault(v => v.GroupName == context.DocumentName);
        document.Info ??= new OpenApiInfo();
        document.Info.Title = Titulo;
        document.Info.Version = versao?.ApiVersion.ToString() ?? context.DocumentName;
        document.Info.Contact = new OpenApiContact { Name = "Time OrderFlow", Email = "api@orderflow.dev" };

        var descricao = "API de pedidos do OrderFlow. Erros seguem ProblemDetails (RFC 9457) com as extensões `code` e `traceId`.";
        if (versao is { IsDeprecated: true })
        {
            descricao += " **Esta versão está depreciada**";
            if (versao.DeprecationPolicy?.Date is { } depreciadaEm)
                descricao += $" desde {depreciadaEm.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
            if (versao.SunsetPolicy?.Date is { } sunset)
                descricao += $" e deixará de responder em {sunset.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} (sunset)";
            descricao += ". Migre para a versão mais recente.";
        }
        document.Info.Description = descricao;
        return Task.CompletedTask;
    }
}

/// <summary>Document transformer: declara o esquema de segurança Bearer (JWT) e o exige no documento todo.</summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public const string NomeEsquema = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[NomeEsquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token JWT emitido pelo Entra ID (módulo 5.04). Envie em Authorization: Bearer <token>.",
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(NomeEsquema, document)] = [],
        });
        return Task.CompletedTask;
    }
}

/// <summary>Operation transformer: marca como <c>deprecated: true</c> as operações de versões depreciadas.</summary>
public sealed class DeprecatedOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);
        if (context.Description.IsDeprecated) operation.Deprecated = true;
        return Task.CompletedTask;
    }
}

/// <summary>Schema transformer: exemplos nos schemas que mais confundem (dinheiro e criação de pedido).</summary>
public sealed class ExemplosSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (context.JsonTypeInfo.Type == typeof(DinheiroResponse))
        {
            schema.Examples = [new JsonObject { ["valor"] = 620.00m, ["moeda"] = "BRL" }];
        }
        else if (context.JsonTypeInfo.Type == typeof(CriarPedidoV2Request))
        {
            schema.Examples =
            [
                new JsonObject
                {
                    ["clienteId"] = "aaaaaaaa-0000-0000-0000-000000000001",
                    ["itens"] = new JsonArray(new JsonObject { ["produtoId"] = "11111111-1111-1111-1111-111111111111", ["quantidade"] = 2 }),
                },
            ];
        }
        return Task.CompletedTask;
    }
}
