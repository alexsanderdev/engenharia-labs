using System.ComponentModel.DataAnnotations;

namespace F1M08.Config;

/// <summary>Configurações do catálogo do OrderFlow (seção "Catalog").</summary>
public sealed class CatalogOptions
{
    public const string SectionName = "Catalog";

    /// <summary>Nome exibido da loja. Obrigatório.</summary>
    [Required]
    public string StoreName { get; set; } = "";

    /// <summary>Tamanho máximo de página aceito na listagem (1 a 200).</summary>
    [Range(1, 200)]
    public int MaxPageSize { get; set; } = 50;

    /// <summary>Tamanho de página padrão (1 a 200). Não pode ser maior que <see cref="MaxPageSize"/>.</summary>
    [Range(1, 200)]
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>Aumento máximo de preço permitido numa única alteração, em % (0 a 100).</summary>
    [Range(typeof(decimal), "0", "100")]
    public decimal MaxPriceIncreasePercent { get; set; } = 30;

    /// <summary>
    /// SEGREDO: chave da API de preços. Vem de user-secrets (dev) ou variável de ambiente/Key Vault (prod).
    /// Nunca em appsettings.json versionado, nunca em log.
    /// </summary>
    [Required]
    public string PricingApiKey { get; set; } = "";
}
