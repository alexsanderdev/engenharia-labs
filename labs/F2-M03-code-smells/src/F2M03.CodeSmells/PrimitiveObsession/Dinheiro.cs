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
    public Dinheiro(decimal valor) =>
        throw new NotImplementedException("TODO: rejeite negativo (ArgumentException) e guarde em Valor arredondado para 2 casas com MidpointRounding.AwayFromZero.");

    /// <summary>R$ 0,00.</summary>
    public static Dinheiro Zero => throw new NotImplementedException("TODO: devolva new(0m).");

    /// <summary>Soma dois valores.</summary>
    public static Dinheiro operator +(Dinheiro a, Dinheiro b) => throw new NotImplementedException("TODO: some os valores.");

    /// <summary>Multiplica por uma quantidade (ex.: preço unitário × quantidade).</summary>
    /// <exception cref="ArgumentException">Quantidade negativa.</exception>
    public static Dinheiro operator *(Dinheiro preco, int quantidade) =>
        throw new NotImplementedException("TODO: multiplique pela quantidade (negativa lança ArgumentException).");
}
