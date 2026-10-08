namespace F2M03.CodeSmells.ShotgunSurgery;

/// <summary>
/// Carrinho de compras: acumula quantidades por SKU.
/// SMELL: Shotgun Surgery — a regra "1 a 10" está aqui, no <see cref="Checkout"/> e no
/// <see cref="ImportadorDePedidosCsv"/>. Mudar o limite exige caçar o 10 em três lugares.
/// </summary>
public sealed class Carrinho
{
    private readonly Dictionary<string, int> _itens = [];

    public Carrinho() { }

    /// <summary>Carrinho com uma política de quantidade explícita.</summary>
    public Carrinho(PoliticaDeQuantidade politica) =>
        throw new NotImplementedException("TODO: guarde a política e faça o construtor sem parâmetros usar PoliticaDeQuantidade.Padrao.");

    /// <summary>Itens atuais (SKU → quantidade).</summary>
    public IReadOnlyDictionary<string, int> Itens => _itens;

    /// <summary>
    /// Adiciona unidades de um SKU. Tanto a quantidade adicionada quanto o total acumulado do SKU
    /// precisam respeitar a política.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Quantidade ou total fora da política.</exception>
    public void Adicionar(string sku, int quantidade)
    {
        if (quantidade < 1 || quantidade > 10)
            throw new ArgumentOutOfRangeException(nameof(quantidade), quantidade, "Quantidade deve estar entre 1 e 10.");

        var total = _itens.GetValueOrDefault(sku) + quantidade;
        if (total > 10)
            throw new ArgumentOutOfRangeException(nameof(quantidade), total, "Quantidade deve estar entre 1 e 10.");

        _itens[sku] = total;
    }
}
