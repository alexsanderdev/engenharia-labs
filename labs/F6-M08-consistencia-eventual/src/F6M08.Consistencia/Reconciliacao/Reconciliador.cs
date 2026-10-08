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
        var agora = relogio.GetUtcNow();
        var tolerancia = opcoes.Value.ToleranciaDaReconciliacao;
        var naFonte = fonte.Listar();
        var corrigidas = new List<Divergencia>();
        var adiadas = 0;

        foreach (var verdade in naFonte)
        {
            var projetado = projecao.ObterPedido(verdade.PedidoId);
            var tipo = Classificar(verdade, projetado);
            if (tipo is null) continue;

            if (agora - verdade.AtualizadoEm < tolerancia)
            {
                adiadas++;
                continue;
            }

            projecao.Corrigir(new PedidoResumido(verdade.PedidoId, verdade.ClienteId, verdade.Status, verdade.Total, verdade.Versao));
            corrigidas.Add(new Divergencia(verdade.PedidoId, tipo.Value, projetado?.Versao ?? 0, verdade.Versao));
        }

        var idsDaFonte = naFonte.Select(p => p.PedidoId).ToHashSet();
        foreach (var fantasma in projecao.Todos().Where(p => !idsDaFonte.Contains(p.PedidoId)))
        {
            // Cuidado em produção: a leitura da fonte acima é um snapshot; um pedido criado DEPOIS dele
            // apareceria aqui como "fantasma". Por isso relemos a fonte antes de remover.
            if (fonte.Obter(fantasma.PedidoId) is not null) continue;
            if (projecao.Remover(fantasma.PedidoId))
                corrigidas.Add(new Divergencia(fantasma.PedidoId, TipoDeDivergencia.Fantasma, fantasma.Versao, 0));
        }

        return new RelatorioDeReconciliacao(naFonte.Count, corrigidas, adiadas);
    }

    private static TipoDeDivergencia? Classificar(PedidoNaFonte verdade, PedidoResumido? projetado) => projetado switch
    {
        null => TipoDeDivergencia.Ausente,
        _ when projetado.Versao < verdade.Versao => TipoDeDivergencia.Atrasado,
        _ when projetado.Versao > verdade.Versao => null, // a fonte mudou depois do nosso snapshot: nada a fazer
        _ when projetado.Status != verdade.Status || projetado.Total != verdade.Total || projetado.ClienteId != verdade.ClienteId
            => TipoDeDivergencia.Divergente,
        _ => null,
    };
}
