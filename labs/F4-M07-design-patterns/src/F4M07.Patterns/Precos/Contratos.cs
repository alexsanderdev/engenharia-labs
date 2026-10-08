namespace F4M07.Patterns.Precos;

/// <summary>Consulta o preço atual de um SKU. <c>null</c> = SKU não existe no catálogo.</summary>
public interface IServicoDePrecos
{
    ValueTask<decimal?> ObterPrecoAsync(string sku, CancellationToken ct = default);
}

/// <summary>Opções do cache de preços (seção "Precos:Cache").</summary>
public sealed class OpcoesDoCacheDePrecos
{
    /// <summary>Tempo de vida de um preço no cache. Padrão: 5 minutos.</summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(5);
}
