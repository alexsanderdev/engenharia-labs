using F6M08.Consistencia.Consistencia;
using F6M08.Consistencia.Fonte;
using F6M08.Consistencia.Projecao;
using Microsoft.Extensions.Options;

namespace F6M08.Consistencia.Reconciliacao;

public enum TipoDeDivergencia
{
    /// <summary>Existe na fonte e não existe na projeção (ex.: PedidoCriado perdido).</summary>
    Ausente,

    /// <summary>Projeção numa versão menor que a da fonte (evento perdido ou lacuna presa no buffer).</summary>
    Atrasado,

    /// <summary>Mesma versão, dados diferentes (bug no projetor). O mais grave: nenhum evento futuro corrige.</summary>
    Divergente,

    /// <summary>Existe na projeção e não existe na fonte (evento publicado de uma escrita que não foi confirmada).</summary>
    Fantasma,
}

public sealed record Divergencia(Guid PedidoId, TipoDeDivergencia Tipo, long VersaoNaProjecao, long VersaoNaFonte);

/// <param name="Verificados">Quantos pedidos da fonte foram comparados.</param>
/// <param name="Corrigidas">Divergências encontradas E corrigidas nesta execução.</param>
/// <param name="AdiadasPorTolerancia">Divergências ignoradas porque a fonte mudou há menos que a tolerância (provavelmente em trânsito).</param>
public sealed record RelatorioDeReconciliacao(int Verificados, IReadOnlyList<Divergencia> Corrigidas, int AdiadasPorTolerancia);

/// <summary>
/// Job de reconciliação: compara a FONTE DA VERDADE com a projeção e corrige a projeção.
/// <para>
/// A fonte sempre vence. A projeção nunca anda para trás. Divergência recente (fonte alterada há menos que
/// <see cref="ConsistenciaOptions.ToleranciaDaReconciliacao"/>) é deixada para o fluxo normal de eventos.
/// </para>
/// </summary>
public sealed class Reconciliador(
    FonteDePedidos fonte,
    ProjecaoResumoDoCliente projecao,
    IOptions<ConsistenciaOptions> opcoes,
    TimeProvider relogio)
{
    public RelatorioDeReconciliacao Reconciliar()
    {
        // TODO (Passo 4):
        //   1. Para cada pedido de fonte.Listar(): compare com projecao.ObterPedido(...) e classifique
        //      (Ausente, Atrasado, Divergente; projeção À FRENTE do snapshot da fonte não é divergência).
        //   2. Divergência com fonte alterada há menos que ToleranciaDaReconciliacao → só conte em AdiadasPorTolerancia.
        //   3. Senão → projecao.Corrigir(estado da fonte) e registre a Divergencia.
        //   4. Pedidos que só existem na projeção → Fantasma: confirme na fonte (fonte.Obter) e remova.
        _ = (fonte, projecao, opcoes, relogio);
        throw new NotImplementedException("TODO: Passo 4 — compare fonte e projeção, respeite a tolerância e corrija a projeção.");
    }
}
