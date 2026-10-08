namespace F2M03.CodeSmells.SwitchStatements;

/// <summary>Tipos de pagamento aceitos (usado pelos chamadores antigos e pela fábrica).</summary>
public enum TipoDePagamento
{
    Pix,
    Boleto,
    CartaoDeCredito,
}

/// <summary>
/// Cada meio de pagamento sabe a própria taxa, prazo e descrição. Um meio novo é uma classe nova,
/// não um "case" a mais em vários switches.
/// </summary>
public abstract class MeioDePagamento
{
    /// <summary>Taxa cobrada sobre o valor, em reais, com 2 casas.</summary>
    public abstract decimal CalcularTaxa(decimal valor);

    /// <summary>Dias até o dinheiro cair na conta.</summary>
    public abstract int DiasParaCompensar { get; }

    /// <summary>Texto exibido ao cliente.</summary>
    public abstract string Descricao { get; }

    /// <summary>
    /// Fábrica: o ÚNICO switch sobre o tipo que sobra no sistema. <paramref name="parcelas"/> só vale para cartão.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Tipo desconhecido ou parcelas fora de 1..12 no cartão.</exception>
    public static MeioDePagamento Criar(TipoDePagamento tipo, int parcelas = 1) => tipo switch
    {
        TipoDePagamento.Pix => new Pix(),
        TipoDePagamento.Boleto => new Boleto(),
        TipoDePagamento.CartaoDeCredito => new CartaoDeCredito(parcelas),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de pagamento desconhecido."),
    };
}

/// <summary>Pix: sem taxa, compensação imediata.</summary>
public sealed class Pix : MeioDePagamento
{
    public override decimal CalcularTaxa(decimal valor) => 0m;
    public override int DiasParaCompensar => 0;
    public override string Descricao => "Pix";
}

/// <summary>Boleto: taxa fixa de R$ 3,50, compensa em 2 dias.</summary>
public sealed class Boleto : MeioDePagamento
{
    public override decimal CalcularTaxa(decimal valor) => 3.50m;
    public override int DiasParaCompensar => 2;
    public override string Descricao => "Boleto bancário";
}

/// <summary>Cartão de crédito: 2,99% + 0,5% por parcela adicional; compensa em 30 dias.</summary>
public sealed class CartaoDeCredito : MeioDePagamento
{
    public int Parcelas { get; }

    /// <exception cref="ArgumentOutOfRangeException">Parcelas fora de 1..12.</exception>
    public CartaoDeCredito(int parcelas)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(parcelas, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(parcelas, 12);
        Parcelas = parcelas;
    }

    public override decimal CalcularTaxa(decimal valor) =>
        Math.Round(valor * (0.0299m + 0.005m * (Parcelas - 1)), 2, MidpointRounding.AwayFromZero);

    public override int DiasParaCompensar => 30;
    public override string Descricao => $"Cartão de crédito em {Parcelas}x";
}
