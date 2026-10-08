namespace MP2.OrderCalc.Dominio;

/// <summary>Quantidade de um item: sempre maior que zero.</summary>
public readonly record struct Quantidade
{
    private Quantidade(int valor) => Valor = valor;

    public int Valor { get; }

    public static Quantidade Criar(int valor) =>
        TentarCriar(valor, out var quantidade)
            ? quantidade
            : throw new ArgumentOutOfRangeException(nameof(valor), valor, "Quantidade deve ser maior que zero.");

    public static bool TentarCriar(int valor, out Quantidade quantidade)
    {
        quantidade = new Quantidade(valor);
        return valor > 0;
    }

    public static Money operator *(Money preco, Quantidade quantidade) => preco.Vezes(quantidade.Valor);

    public override string ToString() => Valor.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
