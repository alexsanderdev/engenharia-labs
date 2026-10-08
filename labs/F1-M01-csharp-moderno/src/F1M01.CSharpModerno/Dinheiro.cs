using System.Globalization;

namespace F1M01.CSharpModerno;

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
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        ArgumentException.ThrowIfNullOrWhiteSpace(moeda);

        Valor = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        Moeda = moeda.Trim().ToUpperInvariant();
    }

    /// <summary>Atalho para valores em reais.</summary>
    public static Dinheiro Reais(decimal valor) => new(valor, "BRL");

    /// <summary>Zero na moeda informada.</summary>
    public static Dinheiro Zero(string moeda = "BRL") => new(0m, moeda);

    /// <summary>Soma dois valores. Moedas diferentes lançam <see cref="InvalidOperationException"/>.</summary>
    public static Dinheiro operator +(Dinheiro a, Dinheiro b)
    {
        GarantirMesmaMoeda(a, b);
        return new(a.Valor + b.Valor, a.Moeda);
    }

    /// <summary>
    /// Subtrai dois valores. Moedas diferentes lançam <see cref="InvalidOperationException"/>;
    /// resultado negativo lança <see cref="InvalidOperationException"/>.
    /// </summary>
    public static Dinheiro operator -(Dinheiro a, Dinheiro b)
    {
        GarantirMesmaMoeda(a, b);
        var resultado = a.Valor - b.Valor;
        if (resultado < 0)
            throw new InvalidOperationException("O resultado da subtração não pode ser negativo.");
        return new(resultado, a.Moeda);
    }

    /// <summary>Multiplica pelo fator (ex.: quantidade). O resultado é arredondado.</summary>
    public static Dinheiro operator *(Dinheiro dinheiro, decimal fator) => new(dinheiro.Valor * fator, dinheiro.Moeda);

    /// <summary>Retorna o percentual do valor (0.10m = 10%).</summary>
    public Dinheiro Percentual(decimal percentual) => this * percentual;

    /// <summary>Formato invariável: "BRL 15.50".</summary>
    public override string ToString() =>
        $"{Moeda} {Valor.ToString("0.00", CultureInfo.InvariantCulture)}";

    private static void GarantirMesmaMoeda(Dinheiro a, Dinheiro b)
    {
        if (a.Moeda != b.Moeda)
            throw new InvalidOperationException($"Não é possível operar {a.Moeda} com {b.Moeda}.");
    }
}
