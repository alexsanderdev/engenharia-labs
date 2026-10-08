namespace F2M03.CodeSmells.ShotgunSurgery;

/// <summary>Checkout: confere o carrinho antes de criar o pedido.</summary>
public sealed class Checkout
{
    public Checkout() { }

    /// <summary>Checkout com uma política de quantidade explícita.</summary>
    public Checkout(PoliticaDeQuantidade politica) =>
        throw new NotImplementedException("TODO: guarde a política e faça o construtor sem parâmetros usar PoliticaDeQuantidade.Padrao.");

    /// <summary>Pode finalizar se houver itens e todas as quantidades respeitarem a política.</summary>
    public bool PodeFinalizar(IReadOnlyDictionary<string, int> itens)
    {
        ArgumentNullException.ThrowIfNull(itens);
        return itens.Count > 0 && itens.Values.All(q => q >= 1 && q <= 10); // o mesmo 10, de novo
    }
}
