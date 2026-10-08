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
    public static MeioDePagamento Criar(TipoDePagamento tipo, int parcelas = 1) =>
        throw new NotImplementedException("TODO: switch expression devolvendo Pix, Boleto ou CartaoDeCredito(parcelas); tipo desconhecido lança ArgumentOutOfRangeException.");
}

/// <summary>Pix: sem taxa, compensação imediata.</summary>
public sealed class Pix : MeioDePagamento
{
    public override decimal CalcularTaxa(decimal valor) => throw new NotImplementedException("TODO: regra do Pix.");
    public override int DiasParaCompensar => throw new NotImplementedException("TODO: regra do Pix.");
    public override string Descricao => throw new NotImplementedException("TODO: regra do Pix.");
}

/// <summary>Boleto: taxa fixa de R$ 3,50, compensa em 2 dias.</summary>
public sealed class Boleto : MeioDePagamento
{
    public override decimal CalcularTaxa(decimal valor) => throw new NotImplementedException("TODO: regra do boleto.");
    public override int DiasParaCompensar => throw new NotImplementedException("TODO: regra do boleto.");
    public override string Descricao => throw new NotImplementedException("TODO: regra do boleto.");
}

/// <summary>Cartão de crédito: 2,99% + 0,5% por parcela adicional; compensa em 30 dias.</summary>
public sealed class CartaoDeCredito : MeioDePagamento
{
    public int Parcelas { get; }

    /// <exception cref="ArgumentOutOfRangeException">Parcelas fora de 1..12.</exception>
    public CartaoDeCredito(int parcelas) =>
        throw new NotImplementedException("TODO: valide 1..12 (ArgumentOutOfRangeException) e guarde as parcelas.");

    public override decimal CalcularTaxa(decimal valor) => throw new NotImplementedException("TODO: regra do cartão.");
    public override int DiasParaCompensar => throw new NotImplementedException("TODO: regra do cartão.");
    public override string Descricao => throw new NotImplementedException("TODO: regra do cartão.");
}
