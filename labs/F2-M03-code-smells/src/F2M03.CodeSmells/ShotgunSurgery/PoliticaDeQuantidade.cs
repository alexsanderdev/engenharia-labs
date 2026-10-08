namespace F2M03.CodeSmells.ShotgunSurgery;

/// <summary>
/// A regra "quantidade por item entre 1 e o máximo" num lugar só. Mudar o máximo passa a ser
/// UMA alteração, não uma caça ao número 10 espalhado pelo código (Shotgun Surgery).
/// </summary>
public sealed class PoliticaDeQuantidade
{
    /// <summary>Política atual do OrderFlow: até 10 unidades por item.</summary>
    public static PoliticaDeQuantidade Padrao { get; } = new(10);

    /// <summary>Máximo de unidades de um mesmo SKU.</summary>
    public int MaximoPorItem { get; }

    public PoliticaDeQuantidade(int maximoPorItem) => MaximoPorItem = maximoPorItem;

    /// <summary>Verdadeiro se 1 ≤ quantidade ≤ <see cref="MaximoPorItem"/>.</summary>
    public bool Permite(int quantidade) => quantidade >= 1 && quantidade <= MaximoPorItem;

    /// <summary>Lança se a quantidade não for permitida.</summary>
    /// <exception cref="ArgumentOutOfRangeException">Mensagem: "Quantidade deve estar entre 1 e {máximo}."</exception>
    public void Validar(int quantidade)
    {
        if (!Permite(quantidade))
            throw new ArgumentOutOfRangeException(nameof(quantidade), quantidade, $"Quantidade deve estar entre 1 e {MaximoPorItem}.");
    }
}
