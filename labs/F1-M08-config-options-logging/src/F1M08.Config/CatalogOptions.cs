using System.ComponentModel.DataAnnotations;

namespace F1M08.Config;

// TODO: use System.ComponentModel.DataAnnotations ([Required], [Range]) nas propriedades abaixo.

/// <summary>Configurações do catálogo do OrderFlow (seção "Catalog").</summary>
public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    /// <summary>Nome exibido da loja. Obrigatório.</summary>
    // TODO: atributo de validação (ver summary)
    public string StoreName { get; set; } = "";

    /// <summary>Tamanho máximo de página aceito na listagem (1 a 200).</summary>
    // TODO: atributo de validação (ver summary)
    public int MaxPageSize { get; set; } = 50;

    /// <summary>Tamanho de página padrão (1 a 200). Não pode ser maior que <see cref="MaxPageSize"/>.</summary>
    // TODO: atributo de validação (ver summary)
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>Aumento máximo de preço permitido numa única alteração, em % (0 a 100).</summary>
    // TODO: atributo de validação (ver summary)
    public decimal MaxPriceIncreasePercent { get; set; } = 30;

    /// <summary>
    /// SEGREDO: chave da API de preços. Vem de user-secrets (dev) ou variável de ambiente/Key Vault (prod).
    /// Nunca em appsettings.json versionado, nunca em log.
    /// </summary>
    // TODO: atributo de validação (ver summary)
    public string PricingApiKey { get; set; } = "";
}
