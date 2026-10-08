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
    public async Task<LeituraDoResumo> ObterResumoAsync(Guid clienteId, TokenDeConsistencia? token, CancellationToken ct = default)
    {
        if (token is not { } exigido)
            return new LeituraDoResumo(projecao.ObterResumo(clienteId), OrigemDaLeitura.Projecao);

        using var cancelamento = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var alcancou = projecao.AguardarVersaoAsync(exigido.PedidoId, exigido.Versao, cancelamento.Token);

        if (!alcancou.IsCompleted)
        {
            var limite = Task.Delay(opcoes.Value.EsperaMaximaDaLeitura, relogio, cancelamento.Token);
            await Task.WhenAny(alcancou, limite);
            ct.ThrowIfCancellationRequested();
            cancelamento.Cancel(); // libera a espera ou o timer que perdeu a corrida

            if (!alcancou.IsCompletedSuccessfully)
                return new LeituraDoResumo(ResumoDaFonte(clienteId), OrigemDaLeitura.Fonte);
        }

        return new LeituraDoResumo(projecao.ObterResumo(clienteId), OrigemDaLeitura.Projecao);
    }

    private ResumoDoCliente ResumoDaFonte(Guid clienteId) => new(clienteId,
        [.. fonte.ListarDoCliente(clienteId)
            .Select(p => new PedidoResumido(p.PedidoId, p.ClienteId, p.Status, p.Total, p.Versao))
            .OrderBy(p => p.PedidoId)]);
}
