using Asp.Versioning;

namespace F5M02.Api.Configuracao;

/// <summary>
/// Versionamento da API com Asp.Versioning. Estratégia escolhida: SEGMENTO DE URL (<c>/v1/...</c>, <c>/v2/...</c>).
/// Por quê: é explícito em logs, links e no navegador; funciona com qualquer cache/CDN (a URL é a chave);
/// gera um documento OpenAPI por versão sem truque; é o que gateways (APIM) e clientes gerados entendem melhor.
/// Custo: a "mesma" entidade tem URLs diferentes por versão (purista de REST torce o nariz) e a versão vaza para links.
/// Alternativas (header <c>api-version</c>, media type <c>application/json;v=2</c>, query string) estão na Aula.
/// </summary>
public static class Versionamento
{
    public static readonly ApiVersion V1 = new(1, 0);
    public static readonly ApiVersion V2 = new(2, 0);

    /// <summary>Data a partir da qual a v1 é considerada depreciada (header <c>Deprecation</c>, RFC 9745).</summary>
    public static readonly DateTimeOffset DepreciacaoV1 = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Data em que a v1 deixa de responder (header <c>Sunset</c>, RFC 8594).</summary>
    public static readonly DateTimeOffset SunsetV1 = new(2027, 6, 30, 0, 0, 0, TimeSpan.Zero);

    public const string LinkPoliticaV1 = "https://docs.orderflow.dev/api/politica-de-versoes#v1";

    public static IServiceCollection AddVersionamentoDaApi(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = V2;
                options.AssumeDefaultVersionWhenUnspecified = false; // na URL a versão é obrigatória
                options.ReportApiVersions = true;                    // api-supported-versions / api-deprecated-versions
                options.ApiVersionReader = new UrlSegmentApiVersionReader();

                options.Policies.Deprecate(V1)
                    .Effective(DepreciacaoV1)
                    .Link(LinkPoliticaV1).Title("Política de depreciação da v1").Type("text/html");

                options.Policies.Sunset(V1)
                    .Effective(SunsetV1)
                    .Link(LinkPoliticaV1).Title("Política de depreciação da v1").Type("text/html");
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'V";          // 1.0 → "v1"; é o nome do documento OpenAPI
                options.SubstituteApiVersionInUrl = true;  // /v{version}/pedidos → /v1/pedidos no documento
            });

        return services;
    }
}
