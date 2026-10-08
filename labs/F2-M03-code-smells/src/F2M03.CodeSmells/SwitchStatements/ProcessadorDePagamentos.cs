using System.Globalization;

namespace F2M03.CodeSmells.SwitchStatements;

/// <summary>
/// SMELL (antes): Switch Statements repetidos. Os três métodos legados tinham, cada um, um <c>switch</c>
/// sobre <see cref="TipoDePagamento"/>; um meio novo exigia mexer nos três. Agora eles delegam
/// para <see cref="MeioDePagamento"/>.
/// </summary>
public sealed class ProcessadorDePagamentos
{
    /// <summary>LEGADO: taxa por tipo. <paramref name="parcelas"/> só importa para cartão.</summary>
    public decimal CalcularTaxa(TipoDePagamento tipo, decimal valor, int parcelas) =>
        MeioDePagamento.Criar(tipo, parcelas).CalcularTaxa(valor);

    /// <summary>LEGADO: dias de compensação por tipo.</summary>
    public int DiasParaCompensar(TipoDePagamento tipo) => MeioDePagamento.Criar(tipo).DiasParaCompensar;

    /// <summary>LEGADO: descrição por tipo.</summary>
    public string Descrever(TipoDePagamento tipo, int parcelas) => MeioDePagamento.Criar(tipo, parcelas).Descricao;

    /// <summary>
    /// Resumo para o cliente, para QUALQUER meio (inclusive os que ainda não existem):
    /// "Descrição: taxa R$ 0.00, compensa em N dia(s)" (ponto decimal, cultura invariante).
    /// </summary>
    public static string Resumir(MeioDePagamento meio, decimal valor)
    {
        ArgumentNullException.ThrowIfNull(meio);
        var taxa = meio.CalcularTaxa(valor).ToString("0.00", CultureInfo.InvariantCulture);
        return $"{meio.Descricao}: taxa R$ {taxa}, compensa em {meio.DiasParaCompensar} dia(s)";
    }
}
