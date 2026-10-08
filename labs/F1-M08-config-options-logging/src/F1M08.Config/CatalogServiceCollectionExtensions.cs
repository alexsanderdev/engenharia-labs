using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace F1M08.Config;

public static class CatalogServiceCollectionExtensions
{
    public const string PageSizeRuleMessage = "DefaultPageSize não pode ser maior que MaxPageSize.";

    /// <summary>
    /// Registra <see cref="CatalogOptions"/> ligado à seção "Catalog" com:
    /// - ValidateDataAnnotations() (atributos [Required]/[Range]);
    /// - Validate(...) para a regra entre campos DefaultPageSize &lt;= MaxPageSize, com a mensagem <see cref="PageSizeRuleMessage"/>;
    /// - ValidateOnStart() para o host falhar na inicialização, e não na primeira requisição.
    /// Registra também <see cref="ProductPriceService"/> como singleton.
    /// </summary>
    public static IServiceCollection AddCatalog(this IServiceCollection services, IConfiguration configuration)
    {
        throw new NotImplementedException(
            "TODO: services.AddOptions<CatalogOptions>().Bind(...).ValidateDataAnnotations().Validate(...).ValidateOnStart() e AddSingleton<ProductPriceService>().");
    }
}
