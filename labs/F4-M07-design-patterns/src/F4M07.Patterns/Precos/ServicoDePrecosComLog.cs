using Microsoft.Extensions.Logging;

namespace F4M07.Patterns.Precos;

/// <summary>
/// DECORATOR de log: registra cada consulta (com SKU e duração) e avisa quando o SKU não existe.
/// Sempre delega ao serviço interno — log não muda resultado.
/// </summary>
public sealed partial class ServicoDePrecosComLog(
    IServicoDePrecos interno,
    ILogger<ServicoDePrecosComLog> logger,
    TimeProvider tempo) : IServicoDePrecos
{
    /// <summary>
    /// Mede com <see cref="TimeProvider.GetTimestamp"/>/<see cref="TimeProvider.GetElapsedTime(long)"/>; loga
    /// <c>Information</c> (EventId 4701) com <c>Sku</c> e <c>DuracaoMs</c> quando achou, ou <c>Warning</c>
    /// (EventId 4702) com <c>Sku</c> quando o preço é <c>null</c>.
    /// </summary>
    public ValueTask<decimal?> ObterPrecoAsync(string sku, CancellationToken ct = default)
    {
        _ = logger; // usado pelos métodos [LoggerMessage] abaixo
        throw new NotImplementedException(
            "TODO: inicio = tempo.GetTimestamp(); await interno.ObterPrecoAsync(sku, ct); duração = tempo.GetElapsedTime(inicio). " +
            $"Depois chame LogPrecoObtido ou LogPrecoNaoEncontrado. Dependências: {interno.GetType().Name}, {tempo.GetType().Name}.");
    }

    // Os métodos de log já estão prontos (source generator [LoggerMessage]): só chame.
    [LoggerMessage(EventId = 4701, Level = LogLevel.Information, Message = "Preço do {Sku} obtido em {DuracaoMs} ms")]
    private partial void LogPrecoObtido(string sku, double duracaoMs);

    [LoggerMessage(EventId = 4702, Level = LogLevel.Warning, Message = "Preço do {Sku} não encontrado")]
    private partial void LogPrecoNaoEncontrado(string sku);
}
