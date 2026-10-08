namespace F2M03.CodeSmells.ShotgunSurgery;

/// <summary>Carrinho de compras: acumula quantidades por SKU.</summary>
public sealed class Carrinho
{
    private readonly Dictionary<string, int> _itens = [];
    private readonly PoliticaDeQuantidade _politica;

    /// <summary>Usa a <see cref="PoliticaDeQuantidade.Padrao"/>.</summary>
    public Carrinho() : this(PoliticaDeQuantidade.Padrao) { }

    public Carrinho(PoliticaDeQuantidade politica) => _politica = politica;

    /// <summary>Itens atuais (SKU → quantidade).</summary>
    public IReadOnlyDictionary<string, int> Itens => _itens;

    /// <summary>
    /// Adiciona unidades de um SKU. Tanto a quantidade adicionada quanto o total acumulado do SKU
    /// precisam respeitar a política.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Quantidade ou total fora da política.</exception>
    public void Adicionar(string sku, int quantidade)
    {
        _politica.Validar(quantidade);

        var total = _itens.GetValueOrDefault(sku) + quantidade;
        _politica.Validar(total);

        _itens[sku] = total;
    }
}
