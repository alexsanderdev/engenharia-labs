namespace F2M03.CodeSmells.ShotgunSurgery;

/// <summary>Checkout: confere o carrinho antes de criar o pedido.</summary>
public sealed class Checkout
{
    private readonly PoliticaDeQuantidade _politica;

    /// <summary>Usa a <see cref="PoliticaDeQuantidade.Padrao"/>.</summary>
    public Checkout() : this(PoliticaDeQuantidade.Padrao) { }

    public Checkout(PoliticaDeQuantidade politica) => _politica = politica;

    /// <summary>Pode finalizar se houver itens e todas as quantidades respeitarem a política.</summary>
    public bool PodeFinalizar(IReadOnlyDictionary<string, int> itens)
    {
        ArgumentNullException.ThrowIfNull(itens);
        return itens.Count > 0 && itens.Values.All(_politica.Permite);
    }
}
