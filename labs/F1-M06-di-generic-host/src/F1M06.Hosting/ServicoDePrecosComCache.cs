namespace F1M06.Hosting;

/// <summary>
/// DECORATOR: implementa <see cref="IServicoDePrecos"/> envolvendo outra implementação e acrescentando
/// cache por escopo (por requisição). Registrado como Scoped: cada escopo tem seu próprio cache,
/// então um preço nunca fica "velho" por mais que uma requisição.
/// </summary>
public sealed class ServicoDePrecosComCache(IServicoDePrecos interno) : IServicoDePrecos
{
    private readonly Dictionary<string, decimal> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Devolve do cache se já consultado neste escopo; senão, delega ao serviço interno e guarda.
    /// </summary>
    public decimal ObterPreco(string sku)
    {
        if (_cache.TryGetValue(sku, out var preco))
            return preco;

        preco = interno.ObterPreco(sku);
        _cache[sku] = preco;
        return preco;
    }
}
