using F6M08.Consistencia.Consistencia;
using F6M08.Consistencia.Tests.Infra;
using static F6M08.Consistencia.Tests.Infra.Eventos;

namespace F6M08.Consistencia.Tests;

/// <summary>
/// Passo 3 — read-your-writes com token de consistência.
/// Quem escreveu manda o token; a leitura espera a projeção alcançar (no relógio falso) ou cai para a fonte.
/// </summary>
public sealed class ReadYourWritesTests
{
    [Fact]
    public async Task SemToken_LeDaProjecaoMesmoDesatualizada()
    {
        var cenario = new Cenario();
        cenario.Fonte.Criar(Ana, 50m);

        var leitura = await cenario.Consulta().ObterResumoAsync(Ana, token: null, Ct);

        leitura.Origem.ShouldBe(OrigemDaLeitura.Projecao);
        leitura.Resumo.Pedidos.ShouldBeEmpty("sem token, eventual pura: a escrita ainda não chegou");
    }

    [Fact]
    public async Task ComToken_ProjecaoJaAlcancou_RespondeNaHoraPelaProjecao()
    {
        var cenario = new Cenario();
        var gravacao = cenario.Fonte.Criar(Ana, 50m);
        cenario.Avancar(cenario.Opcoes.AtrasoDaProjecao);

        var tarefa = cenario.Consulta().ObterResumoAsync(Ana, new TokenDeConsistencia(gravacao.PedidoId, gravacao.Versao), Ct);

        tarefa.IsCompletedSuccessfully.ShouldBeTrue("não há o que esperar");
        var leitura = await tarefa;
        leitura.Origem.ShouldBe(OrigemDaLeitura.Projecao);
        leitura.Resumo.Quantidade.ShouldBe(1);
    }

    [Fact]
    public async Task ComToken_AguardaAProjecaoAlcancarEDepoisLeDaProjecao()
    {
        var cenario = new Cenario(atrasoDaProjecao: TimeSpan.FromSeconds(3),
            configurar: o => o.EsperaMaximaDaLeitura = TimeSpan.FromSeconds(5));
        var pedido = cenario.Fonte.Criar(Ana, 50m).PedidoId;
        cenario.Avancar(TimeSpan.FromSeconds(3));
        var gravacao = cenario.Fonte.AdicionarItem(pedido, 25m); // v2, chega em 3 s

        var tarefa = cenario.Consulta().ObterResumoAsync(Ana, new TokenDeConsistencia(pedido, gravacao.Versao), Ct);
        tarefa.IsCompleted.ShouldBeFalse("a projeção ainda está na v1: precisa esperar");

        cenario.Avancar(TimeSpan.FromSeconds(3));

        var leitura = await tarefa.WaitAsync(LimiteDeSeguranca, Ct);
        leitura.Origem.ShouldBe(OrigemDaLeitura.Projecao);
        leitura.Resumo.ValorEmAberto.ShouldBe(75m);
    }

    [Fact]
    public async Task ComToken_EsperaEstoura_LeDaFonteEVeAPropriaEscrita()
    {
        var cenario = new Cenario(atrasoDaProjecao: TimeSpan.FromMinutes(1),
            configurar: o => o.EsperaMaximaDaLeitura = TimeSpan.FromSeconds(2));
        var gravacao = cenario.Fonte.Criar(Ana, 50m);

        var tarefa = cenario.Consulta().ObterResumoAsync(Ana, new TokenDeConsistencia(gravacao.PedidoId, gravacao.Versao), Ct);
        cenario.Avancar(TimeSpan.FromSeconds(1));
        tarefa.IsCompleted.ShouldBeFalse("1 s < espera máxima de 2 s");

        cenario.Avancar(TimeSpan.FromSeconds(1));

        var leitura = await tarefa.WaitAsync(LimiteDeSeguranca, Ct);
        leitura.Origem.ShouldBe(OrigemDaLeitura.Fonte);
        leitura.Resumo.Pedidos.Single().PedidoId.ShouldBe(gravacao.PedidoId);
        cenario.Projecao.ObterResumo(Ana).Pedidos.ShouldBeEmpty("a projeção continua atrasada; só ESTA leitura foi à fonte");
    }
}
