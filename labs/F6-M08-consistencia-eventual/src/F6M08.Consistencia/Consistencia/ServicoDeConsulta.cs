using F6M08.Consistencia.Fonte;
using F6M08.Consistencia.Projecao;
using Microsoft.Extensions.Options;

namespace F6M08.Consistencia.Consistencia;

/// <summary>De onde veio a leitura. Vai para o header <c>X-Read-Source</c> da resposta.</summary>
public enum OrigemDaLeitura
{
    Projecao,
    Fonte,
}

public sealed record LeituraDoResumo(ResumoDoCliente Resumo, OrigemDaLeitura Origem);

/// <summary>
/// Leitura do resumo do cliente com <b>read-your-writes</b> opcional.
/// <list type="bullet">
/// <item>Sem token: lê a projeção como ela está (barato, pode estar atrasada — consistência eventual pura).</item>
/// <item>Com token: espera a projeção alcançar a versão do token por no máximo
/// <see cref="ConsistenciaOptions.EsperaMaximaDaLeitura"/> (medida no <see cref="TimeProvider"/>);
/// se alcançar, lê a projeção; se estourar, monta o resumo a partir da FONTE DA VERDADE.</item>
/// </list>
/// </summary>
public sealed class ServicoDeConsulta(
    ProjecaoResumoDoCliente projecao,
    FonteDePedidos fonte,
    IOptions<ConsistenciaOptions> opcoes,
    TimeProvider relogio)
{
    public Task<LeituraDoResumo> ObterResumoAsync(Guid clienteId, TokenDeConsistencia? token, CancellationToken ct = default)
    {
        // TODO (Passo 3):
        //   1. Sem token → projeção, Origem = Projecao.
        //   2. Com token → projecao.AguardarVersaoAsync(...) correndo contra Task.Delay(EsperaMaximaDaLeitura, relogio, ...).
        //      Quem vencer decide: projeção alcançou → lê a projeção; estourou → monta o resumo com fonte.ListarDoCliente(...),
        //      Origem = Fonte. Cancele quem perdeu (CancellationTokenSource ligado ao ct).
        //   3. Já alcançou antes de esperar? Responda na hora (sem criar timer).
        _ = (projecao, fonte, opcoes, relogio);
        throw new NotImplementedException("TODO: Passo 3 — implemente read-your-writes: esperar a projeção até o limite ou cair para a fonte.");
    }
}
