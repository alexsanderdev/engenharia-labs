namespace F2M03.CodeSmells.SwitchStatements;

/// <summary>
/// SMELL: Switch Statements repetidos. Cada método tem um <c>switch</c> sobre <see cref="TipoDePagamento"/>;
/// um meio novo exige mexer nos três (e em todos os outros switches espalhados pelo sistema).
/// Refatoração: Replace Conditional with Polymorphism — os métodos legados passam a delegar para
/// <see cref="MeioDePagamento"/>.
/// </summary>
public sealed class ProcessadorDePagamentos
{
    /// <summary>LEGADO: taxa por tipo. <paramref name="parcelas"/> só importa para cartão.</summary>
    public decimal CalcularTaxa(TipoDePagamento tipo, decimal valor, int parcelas)
    {
        switch (tipo)
        {
            case TipoDePagamento.Pix:
                return 0m;
            case TipoDePagamento.Boleto:
                return 3.50m;
            case TipoDePagamento.CartaoDeCredito:
                if (parcelas < 1 || parcelas > 12)
                    throw new ArgumentOutOfRangeException(nameof(parcelas));
                return Math.Round(valor * (0.0299m + 0.005m * (parcelas - 1)), 2, MidpointRounding.AwayFromZero);
            default:
                throw new ArgumentOutOfRangeException(nameof(tipo));
        }
    }

    /// <summary>LEGADO: dias de compensação por tipo.</summary>
    public int DiasParaCompensar(TipoDePagamento tipo)
    {
        switch (tipo)
        {
            case TipoDePagamento.Pix:
                return 0;
            case TipoDePagamento.Boleto:
                return 2;
            case TipoDePagamento.CartaoDeCredito:
                return 30;
            default:
                throw new ArgumentOutOfRangeException(nameof(tipo));
        }
    }

    /// <summary>LEGADO: descrição por tipo.</summary>
    public string Descrever(TipoDePagamento tipo, int parcelas)
    {
        switch (tipo)
        {
            case TipoDePagamento.Pix:
                return "Pix";
            case TipoDePagamento.Boleto:
                return "Boleto bancário";
            case TipoDePagamento.CartaoDeCredito:
                if (parcelas < 1 || parcelas > 12)
                    throw new ArgumentOutOfRangeException(nameof(parcelas));
                return $"Cartão de crédito em {parcelas}x";
            default:
                throw new ArgumentOutOfRangeException(nameof(tipo));
        }
    }

    /// <summary>
    /// Resumo para o cliente, para QUALQUER meio (inclusive os que ainda não existem):
    /// "Descrição: taxa R$ 0.00, compensa em N dia(s)" (ponto decimal, cultura invariante).
    /// </summary>
    public static string Resumir(MeioDePagamento meio, decimal valor) =>
        throw new NotImplementedException("TODO: use só os membros de MeioDePagamento (nada de switch) e formate a taxa com CultureInfo.InvariantCulture.");
}
