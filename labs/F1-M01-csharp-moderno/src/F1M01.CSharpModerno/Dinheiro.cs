namespace F1M01.CSharpModerno;

// Dica: no ToString você vai precisar de System.Globalization.CultureInfo.InvariantCulture.

/// <summary>
/// Value object de dinheiro do OrderFlow. Igualdade por valor (record), imutável,
/// sempre com 2 casas decimais e nunca negativo.
/// </summary>
public sealed record Dinheiro
{
    /// <summary>Valor arredondado para 2 casas (MidpointRounding.AwayFromZero).</summary>
    public decimal Valor { get; }

    /// <summary>Código da moeda em maiúsculas (ex.: "BRL").</summary>
    public string Moeda { get; }

    /// <summary>
    /// Cria um valor monetário.
    /// Regras: valor negativo lança <see cref="ArgumentOutOfRangeException"/>;
    /// moeda vazia lança <see cref="ArgumentException"/>; o valor é arredondado para 2 casas
    /// com <see cref="MidpointRounding.AwayFromZero"/> e a moeda é guardada em maiúsculas.
    /// </summary>
    public Dinheiro(decimal valor, string moeda = "BRL")
    {
        // TODO: valide (negativo e moeda vazia), arredonde para 2 casas e normalize a moeda.
        throw new NotImplementedException("TODO: implemente o construtor de Dinheiro (validação, arredondamento AwayFromZero e moeda em maiúsculas)");
    }

    /// <summary>Atalho para valores em reais.</summary>
    public static Dinheiro Reais(decimal valor) => new(valor, "BRL");

    /// <summary>Zero na moeda informada.</summary>
    public static Dinheiro Zero(string moeda = "BRL") => new(0m, moeda);

    /// <summary>Soma dois valores. Moedas diferentes lançam <see cref="InvalidOperationException"/>.</summary>
    public static Dinheiro operator +(Dinheiro a, Dinheiro b) =>
        throw new NotImplementedException("TODO: some os valores; moedas diferentes devem lançar InvalidOperationException");

    /// <summary>
    /// Subtrai dois valores. Moedas diferentes lançam <see cref="InvalidOperationException"/>;
    /// resultado negativo lança <see cref="InvalidOperationException"/>.
    /// </summary>
    public static Dinheiro operator -(Dinheiro a, Dinheiro b) =>
        throw new NotImplementedException("TODO: subtraia os valores; moedas diferentes ou resultado negativo lançam InvalidOperationException");

    /// <summary>Multiplica pelo fator (ex.: quantidade). O resultado é arredondado.</summary>
    public static Dinheiro operator *(Dinheiro dinheiro, decimal fator) =>
        throw new NotImplementedException("TODO: multiplique o valor pelo fator, mantendo a moeda");

    /// <summary>Retorna o percentual do valor (0.10m = 10%).</summary>
    public Dinheiro Percentual(decimal percentual) =>
        throw new NotImplementedException("TODO: retorne o percentual do valor (reaproveite o operador *)");

    /// <summary>Formato invariável: "BRL 15.50".</summary>
    public override string ToString() =>
        throw new NotImplementedException("TODO: formate como \"BRL 15.50\" usando CultureInfo.InvariantCulture");
}
