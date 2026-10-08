using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace F1M08.Config;

/// <summary>
/// Altera preços respeitando o limite de aumento configurado.
/// Usa <see cref="IOptionsMonitor{TOptions}"/>: se a configuração mudar em runtime, o novo limite vale
/// na próxima chamada, sem reiniciar a aplicação.
/// </summary>
public sealed class ProductPriceService(ILogger<ProductPriceService> logger, IOptionsMonitor<CatalogOptions> options)
{
    /// <summary>
    /// Aceita a alteração se newPrice &lt;= oldPrice * (1 + MaxPriceIncreasePercent / 100).
    /// - Aceita: loga <see cref="Log.PriceChanged"/> (Information, EventId 1001) e retorna true.
    /// - Recusa: loga <see cref="Log.PriceIncreaseRejected"/> (Warning, EventId 1002) e retorna false.
    /// Reduções de preço são sempre aceitas.
    /// </summary>
    public bool ChangePrice(Guid productId, decimal oldPrice, decimal newPrice)
    {
        var limitPercent = options.CurrentValue.MaxPriceIncreasePercent;
        var maxAllowed = oldPrice * (1 + limitPercent / 100m);

        if (newPrice > maxAllowed)
        {
            Log.PriceIncreaseRejected(logger, productId, oldPrice, newPrice, limitPercent);
            return false;
        }

        Log.PriceChanged(logger, productId, oldPrice, newPrice);
        return true;
    }

    /// <summary>
    /// Loga (Information, EventId 1000) um resumo da configuração na subida: StoreName, MaxPageSize e
    /// DefaultPageSize. NUNCA inclui o PricingApiKey.
    /// </summary>
    public void LogStartupSummary()
    {
        var o = options.CurrentValue;
        Log.CatalogStarted(logger, o.StoreName, o.MaxPageSize, o.DefaultPageSize);
    }
}

/// <summary>
/// Mensagens de log geradas em tempo de compilação pelo source generator ([LoggerMessage]):
/// sem boxing, sem parse do template a cada chamada e com checagem de IsEnabled embutida.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "Catálogo {StoreName} iniciado (MaxPageSize={MaxPageSize}, DefaultPageSize={DefaultPageSize})")]
    public static partial void CatalogStarted(ILogger logger, string storeName, int maxPageSize, int defaultPageSize);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Preço do produto {ProductId} alterado de {OldPrice} para {NewPrice}")]
    public static partial void PriceChanged(ILogger logger, Guid productId, decimal oldPrice, decimal newPrice);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Aumento de preço do produto {ProductId} recusado: {OldPrice} -> {NewPrice} excede o limite de {LimitPercent}%")]
    public static partial void PriceIncreaseRejected(ILogger logger, Guid productId, decimal oldPrice, decimal newPrice, decimal limitPercent);
}
