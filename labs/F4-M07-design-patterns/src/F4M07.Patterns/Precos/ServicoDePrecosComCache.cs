using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace F4M07.Patterns.Precos;

/// <summary>
/// DECORATOR de cache: implementa <see cref="IServicoDePrecos"/> e envolve outra implementação.
/// Quem consome não sabe que existe cache; quem é decorado não sabe que é decorado.
/// </summary>
/// <remarks>
/// Regras: o SKU é comparado sem diferenciar maiúsculas; um preço vale por <see cref="OpcoesDoCacheDePrecos.Ttl"/>
/// (relógio = <see cref="TimeProvider"/>, para o teste controlar o tempo); "não encontrado" (<c>null</c>) NÃO é
/// guardado — um produto recém-cadastrado precisa aparecer na próxima consulta.
/// </remarks>
public sealed class ServicoDePrecosComCache(
    IServicoDePrecos interno,
    TimeProvider tempo,
    IOptions<OpcoesDoCacheDePrecos> opcoes) : IServicoDePrecos
{
    private readonly ConcurrentDictionary<string, (decimal Preco, DateTimeOffset ExpiraEm)> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public async ValueTask<decimal?> ObterPrecoAsync(string sku, CancellationToken ct = default)
    {
        var agora = tempo.GetUtcNow();
        if (_cache.TryGetValue(sku, out var entrada) && agora < entrada.ExpiraEm)
            return entrada.Preco;

        var preco = await interno.ObterPrecoAsync(sku, ct);
        if (preco is { } valor)
            _cache[sku] = (valor, agora + opcoes.Value.Ttl);
        else
            _cache.TryRemove(sku, out _);

        return preco;
    }
}
