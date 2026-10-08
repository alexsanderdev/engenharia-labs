using System.Globalization;

namespace MP2.OrderCalc.Dominio;

/// <summary>
/// Valor monetário em reais. Imutável, sem moeda estrangeira (YAGNI).
/// Arredondamento: 2 casas com <see cref="MidpointRounding.ToEven"/> (arredondamento bancário),
/// que é o padrão de <c>Math.Round</c> usado pelo legado. O golden master trava isso:
/// trocar para AwayFromZero muda o centavo de alguns pedidos (ex.: 4,485 vira 4,48 e não 4,49).
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    private Money(decimal valor) => Valor = valor;

    public decimal Valor { get; }

    public static Money Zero { get; } = new(0m);

    public static Money Reais(decimal valor) => new(valor);

    public Money Arredondar() => new(Math.Round(Valor, 2, MidpointRounding.ToEven));

    /// <summary>Aplica uma fração (0.05m = 5%) e arredonda.</summary>
    public Money Percentual(decimal fracao) => new Money(Valor * fracao).Arredondar();

    /// <summary>Aplica pontos percentuais (10 = 10%) e arredonda. Mesma conta do legado: valor * pontos / 100.</summary>
    public Money PontosPercentuais(decimal pontos) => new Money(Valor * pontos / 100).Arredondar();

    public Money Vezes(int quantidade) => new(Valor * quantidade);

    public static Money Min(Money a, Money b) => a <= b ? a : b;

    public static Money operator +(Money a, Money b) => new(a.Valor + b.Valor);

    public static Money operator -(Money a, Money b) => new(a.Valor - b.Valor);

    public static bool operator <(Money a, Money b) => a.Valor < b.Valor;

    public static bool operator >(Money a, Money b) => a.Valor > b.Valor;

    public static bool operator <=(Money a, Money b) => a.Valor <= b.Valor;

    public static bool operator >=(Money a, Money b) => a.Valor >= b.Valor;

    public int CompareTo(Money other) => Valor.CompareTo(other.Valor);

    public override string ToString() => Valor.ToString("F2", CultureInfo.InvariantCulture);
}
