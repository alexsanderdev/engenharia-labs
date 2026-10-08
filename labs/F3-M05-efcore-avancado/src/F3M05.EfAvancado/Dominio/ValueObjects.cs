namespace F3M05.EfAvancado.Dominio;

/// <summary>
/// Value object de dinheiro: valor + moeda andam juntos. No banco vira DUAS colunas da
/// tabela do dono (owned type): <c>PrecoValor</c> e <c>PrecoMoeda</c>.
/// </summary>
public sealed record Dinheiro
{
    public Dinheiro(decimal valor, string moeda)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        if (moeda is not { Length: 3 })
            throw new ArgumentException("Moeda deve ter 3 letras (ISO 4217).", nameof(moeda));
        Valor = valor;
        Moeda = moeda.ToUpperInvariant();
    }

    public decimal Valor { get; }
    public string Moeda { get; }

    public static Dinheiro Reais(decimal valor) => new(valor, "BRL");

    public override string ToString() => $"{Moeda} {Valor:N2}";
}

/// <summary>
/// SKU tipado (evita confundir com qualquer string). No banco vira UMA coluna varchar(20)
/// via value conversion.
/// </summary>
public readonly record struct Sku
{
    public Sku(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        if (valor.Length > 20)
            throw new ArgumentException("SKU tem no máximo 20 caracteres.", nameof(valor));
        Valor = valor.ToUpperInvariant();
    }

    public string Valor { get; }

    public override string ToString() => Valor;
}
