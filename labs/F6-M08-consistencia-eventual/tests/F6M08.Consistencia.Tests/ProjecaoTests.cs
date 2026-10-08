using F6M08.Consistencia.Consistencia;
using F6M08.Consistencia.Dominio;
using F6M08.Consistencia.Projecao;
using static F6M08.Consistencia.Tests.Infra.Eventos;

namespace F6M08.Consistencia.Tests;

/// <summary>
/// Passos 1 e 2 — token de consistência e a projeção com versão por agregado.
/// Aqui não há fila nem relógio: os eventos são entregues à mão, na ordem (e na bagunça) que o teste quer.
/// </summary>
public sealed class ProjecaoTests
{
    [Fact]
    public void Token_ToStringETryParse_FazemIdaEVolta()
    {
        var token = new TokenDeConsistencia(Guid.NewGuid(), 7);

        TokenDeConsistencia.TryParse(token.ToString(), out var lido).ShouldBeTrue();

        lido.ShouldBe(token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("3f2504e04f8941d39a0c0305e82c3301")]
    [InlineData("nao-e-guid.2")]
    [InlineData("3f2504e04f8941d39a0c0305e82c3301.x")]
    [InlineData("3f2504e04f8941d39a0c0305e82c3301.0")]
    [InlineData("3f2504e04f8941d39a0c0305e82c3301.-1")]
    [InlineData("3f2504e04f8941d39a0c0305e82c3301.1.2")]
    public void Token_TryParse_TextoInvalido_DevolveFalseSemLancar(string? texto)
    {
        TokenDeConsistencia.TryParse(texto, out _).ShouldBeFalse();
    }

    [Fact]
    public void Aplicar_PedidoCriado_ApareceNoResumoDoCliente()
    {
        var projecao = new ProjecaoResumoDoCliente();
        var pedido = Guid.NewGuid();

        projecao.Aplicar(Criado(pedido, 100m)).ShouldBe(ResultadoDaAplicacao.Aplicado);

        var resumo = projecao.ObterResumo(Ana);
        resumo.Quantidade.ShouldBe(1);
        resumo.ValorEmAberto.ShouldBe(100m);
        resumo.Pedidos.Single().ShouldBe(new PedidoResumido(pedido, Ana, StatusPedido.Criado, 100m, 1));
        projecao.VersaoDoPedido(pedido).ShouldBe(1);
        projecao.ObterResumo(Bruno).Pedidos.ShouldBeEmpty();
    }

    [Fact]
    public void Aplicar_EventoDuplicadoOuAntigo_EhIgnoradoENaoSomaDuasVezes()
    {
        var projecao = new ProjecaoResumoDoCliente();
        var pedido = Guid.NewGuid();
        projecao.Aplicar(Criado(pedido, 100m));
        projecao.Aplicar(Item(pedido, 2, 50m)).ShouldBe(ResultadoDaAplicacao.Aplicado);

        projecao.Aplicar(Item(pedido, 2, 50m)).ShouldBe(ResultadoDaAplicacao.Ignorado);   // at-least-once
        projecao.Aplicar(Criado(pedido, 100m)).ShouldBe(ResultadoDaAplicacao.Ignorado);   // antigo

        projecao.ObterPedido(pedido)!.Total.ShouldBe(150m);
        projecao.VersaoDoPedido(pedido).ShouldBe(2);
    }

    [Fact]
    public void Aplicar_EventoForaDeOrdem_FicaAdiadoAteALacunaSerPreenchida()
    {
        var projecao = new ProjecaoResumoDoCliente();
        var pedido = Guid.NewGuid();
        projecao.Aplicar(Criado(pedido, 100m));

        projecao.Aplicar(Confirmado(pedido, 4)).ShouldBe(ResultadoDaAplicacao.Adiado);
        projecao.Aplicar(Item(pedido, 3, 30m)).ShouldBe(ResultadoDaAplicacao.Adiado);
        projecao.Aplicar(Item(pedido, 3, 30m)).ShouldBe(ResultadoDaAplicacao.Adiado); // duplicata de adiado

        // Enquanto falta a v2, nada depois dela pode ser aplicado (ItemAdicionado é delta!).
        projecao.ObterPedido(pedido).ShouldBe(new PedidoResumido(pedido, Ana, StatusPedido.Criado, 100m, 1));
        projecao.EventosAdiados.ShouldBe(2);

        projecao.Aplicar(Item(pedido, 2, 20m)).ShouldBe(ResultadoDaAplicacao.Aplicado);

        projecao.ObterPedido(pedido).ShouldBe(new PedidoResumido(pedido, Ana, StatusPedido.Confirmado, 150m, 4));
        projecao.EventosAdiados.ShouldBe(0);
    }

    [Fact]
    public void Aplicar_EventoChegaAntesDoPedidoCriado_FicaAdiadoENaoQuebra()
    {
        var projecao = new ProjecaoResumoDoCliente();
        var pedido = Guid.NewGuid();

        projecao.Aplicar(Cancelado(pedido, 2)).ShouldBe(ResultadoDaAplicacao.Adiado);
        projecao.ObterResumo(Ana).Pedidos.ShouldBeEmpty();

        projecao.Aplicar(Criado(pedido, 80m)).ShouldBe(ResultadoDaAplicacao.Aplicado);

        var resumo = projecao.ObterResumo(Ana);
        resumo.Pedidos.Single().Status.ShouldBe(StatusPedido.Cancelado);
        resumo.ValorEmAberto.ShouldBe(0m);
    }

    [Fact]
    public async Task AguardarVersao_SoCompletaQuandoAProjecaoAlcancaAVersao()
    {
        var projecao = new ProjecaoResumoDoCliente();
        var pedido = Guid.NewGuid();
        projecao.Aplicar(Criado(pedido));

        projecao.AguardarVersaoAsync(pedido, 1, Ct).IsCompletedSuccessfully.ShouldBeTrue("versão já alcançada: completa na hora");

        var espera = projecao.AguardarVersaoAsync(pedido, 3, Ct);
        espera.IsCompleted.ShouldBeFalse();

        projecao.Aplicar(Item(pedido, 2, 10m));
        espera.IsCompleted.ShouldBeFalse("v2 ainda não é v3");

        projecao.Aplicar(Confirmado(pedido, 3));
        await espera.WaitAsync(LimiteDeSeguranca, Ct);
    }

    [Fact]
    public async Task AguardarVersao_Cancelada_TerminaComoCancelada()
    {
        var projecao = new ProjecaoResumoDoCliente();
        using var cts = new CancellationTokenSource();

        var espera = projecao.AguardarVersaoAsync(Guid.NewGuid(), 1, cts.Token);
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => espera.WaitAsync(LimiteDeSeguranca, Ct));
    }

    [Fact]
    public void Reconstruir_HistoricoEmbaralhadoEDuplicado_ChegaAoMesmoEstado()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        EventoDePedido[] historico =
        [
            Criado(p1, 100m), Item(p1, 2, 25m), Confirmado(p1, 3),
            Criado(p2, 40m, Bruno), Item(p2, 2, 10m, Bruno), Cancelado(p2, 3, Bruno),
        ];
        var emOrdem = new ProjecaoResumoDoCliente();
        foreach (var e in historico) emOrdem.Aplicar(e);

        var reconstruida = new ProjecaoResumoDoCliente();
        reconstruida.Aplicar(Criado(Guid.NewGuid(), 999m)); // lixo que o rebuild precisa apagar
        var baguncado = historico.Concat(historico).OrderBy(e => e.Versao * -1).ThenBy(e => e.PedidoId).ToList();
        reconstruida.Reconstruir(baguncado);

        reconstruida.Todos().OrderBy(p => p.PedidoId).ShouldBe(emOrdem.Todos().OrderBy(p => p.PedidoId));
        reconstruida.ObterResumo(Ana).ValorConfirmado.ShouldBe(125m);
        reconstruida.EventosAdiados.ShouldBe(0);
    }
}
