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
    // Dica: um ConcurrentDictionary<string, (decimal Preco, DateTimeOffset ExpiraEm)> com StringComparer.OrdinalIgnoreCase.

    public ValueTask<decimal?> ObterPrecoAsync(string sku, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO: hit válido (agora < ExpiraEm) devolve do cache; senão delegue ao serviço interno e só guarde se não for null " +
            $"(ExpiraEm = tempo.GetUtcNow() + Ttl). Dependências: {interno.GetType().Name}, {tempo.GetType().Name}, {opcoes.GetType().Name}.");
}
