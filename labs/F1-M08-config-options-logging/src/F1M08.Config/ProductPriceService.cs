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
    /// - Aceita: loga "PriceChanged" (Information, EventId 1001, campos ProductId, OldPrice, NewPrice) e retorna true.
    /// - Recusa: loga "PriceIncreaseRejected" (Warning, EventId 1002, campos ProductId, OldPrice, NewPrice, LimitPercent) e retorna false.
    /// Reduções de preço são sempre aceitas.
    /// </summary>
    public bool ChangePrice(Guid productId, decimal oldPrice, decimal newPrice)
    {
        _ = (logger, options);
        throw new NotImplementedException(
            "TODO: leia options.CurrentValue (não guarde em campo!), calcule o limite e logue com os métodos [LoggerMessage] da classe Log.");
    }

    /// <summary>
    /// Loga "CatalogStarted" (Information, EventId 1000, campos StoreName, MaxPageSize e DefaultPageSize)
    /// com um resumo da configuração na subida. NUNCA inclui o PricingApiKey.
    /// </summary>
    public void LogStartupSummary()
    {
        throw new NotImplementedException("TODO: logue o resumo com um método [LoggerMessage] — sem o segredo.");
    }
}

/// <summary>
/// Mensagens de log geradas em tempo de compilação pelo source generator ([LoggerMessage]).
/// </summary>
internal static partial class Log
{
    // TODO: declare aqui três métodos "public static partial void ...(ILogger logger, ...)" com
    //       [LoggerMessage(EventId = ..., Level = LogLevel...., Message = "... {ProductId} ...")]:
    //       CatalogStarted (1000), PriceChanged (1001) e PriceIncreaseRejected (1002).
    //       Os nomes entre chaves no template viram os campos estruturados do log.
}
