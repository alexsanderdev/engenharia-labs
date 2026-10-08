using F4M02.Pedidos.Domain.Comum;

namespace F4M02.Pedidos.Domain.ValueObjects;

/// <summary>
/// Value object de dinheiro: valor + moeda (ISO 4217) andam sempre juntos.
/// Imutável, autovalidado, igualdade por valor. Toda operação devolve um NOVO <see cref="Dinheiro"/>.
/// </summary>
/// <remarks>
/// Regras: valor nunca negativo (<see cref="Regras.DinheiroNegativo"/>); moeda com 3 letras, normalizada
/// para maiúsculas e sem espaços nas pontas (<see cref="Regras.MoedaInvalida"/>); operações entre moedas
/// diferentes são proibidas (<see cref="Regras.MoedasDiferentes"/>).
/// </remarks>
public sealed record Dinheiro
{
    /// <exception cref="RegraDeNegocioVioladaException">Valor negativo ou moeda inválida.</exception>
    public Dinheiro(decimal valor, string moeda) =>
        throw new NotImplementedException("TODO (Passo 1): valide (valor >= 0; moeda com 3 letras A-Z após Trim/ToUpperInvariant) lançando RegraDeNegocioVioladaException com o código de Regras; depois guarde Valor e Moeda.");

    public decimal Valor { get; }

    /// <summary>Código ISO 4217 em maiúsculas (ex.: BRL, USD).</summary>
    public string Moeda { get; } = string.Empty;

    public static Dinheiro Zero(string moeda) => new(0m, moeda);

    public static Dinheiro Reais(decimal valor) => new(valor, "BRL");

    public static Dinheiro operator +(Dinheiro esquerda, Dinheiro direita) =>
        throw new NotImplementedException("TODO (Passo 1): mesma moeda (senão Regras.MoedasDiferentes) e devolva um NOVO Dinheiro.");

    /// <exception cref="RegraDeNegocioVioladaException">Se o resultado ficar negativo.</exception>
    public static Dinheiro operator -(Dinheiro esquerda, Dinheiro direita) =>
        throw new NotImplementedException("TODO (Passo 1): mesma moeda; resultado negativo cai na validação do construtor.");

    public static Dinheiro operator *(Dinheiro preco, Quantidade quantidade) =>
        throw new NotImplementedException("TODO (Passo 1): preço × quantidade, na mesma moeda.");

    public static bool operator >(Dinheiro esquerda, Dinheiro direita) =>
        throw new NotImplementedException("TODO (Passo 1): compare valores da MESMA moeda.");

    public static bool operator <(Dinheiro esquerda, Dinheiro direita) =>
        throw new NotImplementedException("TODO (Passo 1): compare valores da MESMA moeda.");

    /// <summary>
    /// Percentual deste valor, arredondado para 2 casas com <see cref="MidpointRounding.AwayFromZero"/>
    /// (ex.: 5% de 33,33 = 1,6665 → 1,67).
    /// </summary>
    public Dinheiro Percentual(decimal percentual) =>
        throw new NotImplementedException("TODO (Passo 1): Math.Round(Valor * percentual / 100m, 2, MidpointRounding.AwayFromZero), na mesma moeda.");

    /// <summary>O menor de dois valores da mesma moeda.</summary>
    public static Dinheiro Menor(Dinheiro a, Dinheiro b) => a < b ? a : b;

    /// <summary>Formato invariante, ex.: <c>BRL 10.00</c>.</summary>
    public override string ToString() =>
        throw new NotImplementedException("TODO (Passo 1): use FormattableString.Invariant com o formato 0.00.");
}
