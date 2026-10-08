using F4M02.Pedidos.Domain.Comum;

namespace F4M02.Pedidos.Domain.ValueObjects;

/// <summary>
/// Value object de dinheiro: valor + moeda (ISO 4217) andam sempre juntos.
/// Imutável, autovalidado, igualdade por valor. Toda operação devolve um NOVO <see cref="Dinheiro"/>.
/// </summary>
/// <remarks>
/// Regras: valor nunca negativo (<see cref="Regras.DinheiroNegativo"/>); moeda com 3 letras, normalizada
/// para maiúsculas (<see cref="Regras.MoedaInvalida"/>); operações entre moedas diferentes são proibidas
/// (<see cref="Regras.MoedasDiferentes"/>).
/// </remarks>
public sealed record Dinheiro
{
    /// <exception cref="RegraDeNegocioVioladaException">Valor negativo ou moeda inválida.</exception>
    public Dinheiro(decimal valor, string moeda)
    {
        if (valor < 0)
            throw new RegraDeNegocioVioladaException(Regras.DinheiroNegativo, $"Dinheiro não pode ser negativo ({valor}).");

        var normalizada = moeda?.Trim().ToUpperInvariant();
        if (normalizada is not { Length: 3 } || !normalizada.All(char.IsAsciiLetterUpper))
            throw new RegraDeNegocioVioladaException(Regras.MoedaInvalida, $"Moeda '{moeda}' inválida: use o código ISO 4217 de 3 letras (ex.: BRL).");

        Valor = valor;
        Moeda = normalizada;
    }

    public decimal Valor { get; }

    /// <summary>Código ISO 4217 em maiúsculas (ex.: BRL, USD).</summary>
    public string Moeda { get; }

    public static Dinheiro Zero(string moeda) => new(0m, moeda);

    public static Dinheiro Reais(decimal valor) => new(valor, "BRL");

    public static Dinheiro operator +(Dinheiro esquerda, Dinheiro direita)
    {
        GarantirMesmaMoeda(esquerda, direita);
        return new(esquerda.Valor + direita.Valor, esquerda.Moeda);
    }

    /// <exception cref="RegraDeNegocioVioladaException">Se o resultado ficar negativo.</exception>
    public static Dinheiro operator -(Dinheiro esquerda, Dinheiro direita)
    {
        GarantirMesmaMoeda(esquerda, direita);
        return new(esquerda.Valor - direita.Valor, esquerda.Moeda);
    }

    public static Dinheiro operator *(Dinheiro preco, Quantidade quantidade)
    {
        ArgumentNullException.ThrowIfNull(preco);
        ArgumentNullException.ThrowIfNull(quantidade);
        return new(preco.Valor * quantidade.Valor, preco.Moeda);
    }

    public static bool operator >(Dinheiro esquerda, Dinheiro direita)
    {
        GarantirMesmaMoeda(esquerda, direita);
        return esquerda.Valor > direita.Valor;
    }

    public static bool operator <(Dinheiro esquerda, Dinheiro direita)
    {
        GarantirMesmaMoeda(esquerda, direita);
        return esquerda.Valor < direita.Valor;
    }

    /// <summary>
    /// Percentual deste valor, arredondado para 2 casas com <see cref="MidpointRounding.AwayFromZero"/>
    /// (ex.: 5% de 33,33 = 1,6665 → 1,67).
    /// </summary>
    public Dinheiro Percentual(decimal percentual)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(percentual);
        return new(Math.Round(Valor * percentual / 100m, 2, MidpointRounding.AwayFromZero), Moeda);
    }

    /// <summary>O menor de dois valores da mesma moeda.</summary>
    public static Dinheiro Menor(Dinheiro a, Dinheiro b) => a < b ? a : b;

    public override string ToString() => FormattableString.Invariant($"{Moeda} {Valor:0.00}");

    private static void GarantirMesmaMoeda(Dinheiro esquerda, Dinheiro direita)
    {
        ArgumentNullException.ThrowIfNull(esquerda);
        ArgumentNullException.ThrowIfNull(direita);
        if (esquerda.Moeda != direita.Moeda)
            throw new RegraDeNegocioVioladaException(Regras.MoedasDiferentes, $"Não dá para operar {esquerda.Moeda} com {direita.Moeda}.");
    }
}
