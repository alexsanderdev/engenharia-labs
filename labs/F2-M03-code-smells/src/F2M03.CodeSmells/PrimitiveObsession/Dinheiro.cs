namespace F2M03.CodeSmells.PrimitiveObsession;

/// <summary>
/// Value object de dinheiro (em reais): nunca negativo e sempre com 2 casas,
/// arredondando "para longe do zero" (0,005 → 0,01), como o legado fazia.
/// </summary>
public readonly record struct Dinheiro
{
    /// <summary>Valor em reais, com 2 casas decimais.</summary>
    public decimal Valor { get; }

    /// <summary>Cria um valor monetário.</summary>
    /// <exception cref="ArgumentException">Valor negativo.</exception>
    public Dinheiro(decimal valor)
    {
        if (valor < 0)
            throw new ArgumentException("Valor monetário não pode ser negativo.", nameof(valor));

        Valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>R$ 0,00.</summary>
    public static Dinheiro Zero => new(0m);

    /// <summary>Soma dois valores.</summary>
    public static Dinheiro operator +(Dinheiro a, Dinheiro b) => new(a.Valor + b.Valor);

    /// <summary>Multiplica por uma quantidade (ex.: preço unitário × quantidade).</summary>
    /// <exception cref="ArgumentException">Quantidade negativa.</exception>
    public static Dinheiro operator *(Dinheiro preco, int quantidade) =>
        quantidade < 0
            ? throw new ArgumentException("Quantidade não pode ser negativa.", nameof(quantidade))
            : new(preco.Valor * quantidade);
}
