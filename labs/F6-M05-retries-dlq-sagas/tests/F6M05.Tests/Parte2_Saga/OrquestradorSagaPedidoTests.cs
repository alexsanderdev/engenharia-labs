using F6M05.Sagas.Retry;
using F6M05.Sagas.Saga;
using F6M05.Tests.Infra;
using Microsoft.Extensions.Time.Testing;

namespace F6M05.Tests.Parte2_Saga;

/// <summary>
/// Passos 7 e 8 — o orquestrador com estado no SQL Server: caminho feliz, compensação, timeout
/// (FakeTimeProvider), duplicatas, mensagens fora de ordem e corrida entre timeout e pagamento.
/// </summary>
public sealed class OrquestradorSagaPedidoTests(InfraFixture infra) : SqlTestBase(infra)
{
    private static readonly OpcoesDaSaga Opcoes = new() { PrazoDoPagamento = TimeSpan.FromSeconds(30) };

    private readonly FakeTimeProvider _relogio = new(Msg.Inicio);
    private readonly PublicadorEmMemoria _publicador = new();
    private readonly Guid _pedido = Guid.NewGuid();

    private RepositorioDeSagasSql Repositorio => new(Infra.Sql);

    private OrquestradorSagaPedido Orquestrador(IRepositorioDeSagas? repositorio = null) =>
        new(repositorio ?? Repositorio, _publicador, _relogio, Opcoes);

    private VerificadorDePrazos Verificador() => new(Repositorio, Orquestrador(), _relogio);

    private async Task<SagaPedido> SagaAsync() => (await Repositorio.ObterAsync(_pedido)).ShouldNotBeNull();

    private async Task LevarAteAguardandoPagamentoAsync()
    {
        await Orquestrador().ProcessarAsync(Msg.PedidoCriado(_pedido));
        await Orquestrador().ProcessarAsync(Msg.EstoqueReservado(_pedido));
    }

    [Fact]
    public async Task CaminhoFeliz_PersisteCadaPassoEPublicaOsComandosNaOrdem()
    {
        var orquestrador = Orquestrador();

        var r1 = await orquestrador.ProcessarAsync(Msg.PedidoCriado(_pedido, 200m));
        r1.Tipo.ShouldBe(TipoDeResultado.Iniciada);
        r1.Versao.ShouldBe(1);

        var r2 = await orquestrador.ProcessarAsync(Msg.EstoqueReservado(_pedido));
        r2.Tipo.ShouldBe(TipoDeResultado.Aplicada);
        r2.Status.ShouldBe(StatusSaga.AguardandoPagamento);

        var r3 = await orquestrador.ProcessarAsync(Msg.PagamentoAutorizado(_pedido));
        r3.ShouldBe(r3 with { Tipo = TipoDeResultado.Aplicada, Status = StatusSaga.Concluida, Versao = 3 });

        _publicador.DoPedido(_pedido).Select(c => c.Tipo).ShouldBe(
            [nameof(ReservarEstoque), nameof(AutorizarPagamento), nameof(ConfirmarPedido)]);
        _publicador.DoPedido(_pedido).OfType<AutorizarPagamento>().Single().Valor.ShouldBe(200m);

        var saga = await SagaAsync();
        saga.Status.ShouldBe(StatusSaga.Concluida);
        saga.Versao.ShouldBe(3);
        saga.Passos.ShouldBe(PassosDaSaga.EstoqueReservado | PassosDaSaga.PagamentoAutorizado | PassosDaSaga.PedidoConfirmado);
    }

    [Fact]
    public async Task PagamentoRecusado_CompensaOEstoqueECancelaOPedido()
    {
        await LevarAteAguardandoPagamentoAsync();

        (await Orquestrador().ProcessarAsync(Msg.PagamentoRecusado(_pedido))).Status.ShouldBe(StatusSaga.Compensando);
        (await Orquestrador().ProcessarAsync(Msg.EstoqueLiberado(_pedido))).Status.ShouldBe(StatusSaga.Cancelada);

        _publicador.DoPedido(_pedido).Select(c => c.Tipo).ShouldBe(
            [nameof(ReservarEstoque), nameof(AutorizarPagamento), nameof(LiberarEstoque), nameof(CancelarPedido)]);
        var saga = await SagaAsync();
        saga.Passos.HasFlag(PassosDaSaga.EstoqueLiberado).ShouldBeTrue();
        saga.MotivoCancelamento.ShouldBe(MotivosDeCancelamento.PagamentoRecusado("cartão sem limite"));
    }

    [Fact]
    public async Task Timeout_PagamentoNaoResponde_VerificadorCompensaSoDepoisDoPrazo()
    {
        await LevarAteAguardandoPagamentoAsync(); // prazo = Inicio + 30 s
        var verificador = Verificador();

        _relogio.Advance(TimeSpan.FromSeconds(29));
        (await verificador.VerificarAsync()).ShouldBe(0, "ainda dentro do prazo");
        (await SagaAsync()).Status.ShouldBe(StatusSaga.AguardandoPagamento);

        _relogio.Advance(TimeSpan.FromSeconds(1));
        (await verificador.VerificarAsync()).ShouldBe(1);
        var saga = await SagaAsync();
        saga.Status.ShouldBe(StatusSaga.Compensando);
        saga.MotivoCancelamento.ShouldBe(MotivosDeCancelamento.PrazoDoPagamentoExpirado);
        saga.JaProcessou(VerificadorDePrazos.MessageIdDoPrazo(_pedido)).ShouldBeTrue();
        _publicador.DoPedido(_pedido)[^1].ShouldBeOfType<LiberarEstoque>();

        (await verificador.VerificarAsync()).ShouldBe(0, "rodar de novo não compensa duas vezes");

        await Orquestrador().ProcessarAsync(Msg.EstoqueLiberado(_pedido));
        (await SagaAsync()).Status.ShouldBe(StatusSaga.Cancelada);
        _publicador.DoPedido(_pedido)[^1].ShouldBeOfType<CancelarPedido>()
            .Motivo.ShouldBe(MotivosDeCancelamento.PrazoDoPagamentoExpirado);
    }

    [Fact]
    public async Task Duplicatas_NaoMudamAEstadoERepublicamComOMesmoMessageId()
    {
        var criado = Msg.PedidoCriado(_pedido);
        var reservado = Msg.EstoqueReservado(_pedido);

        await Orquestrador().ProcessarAsync(criado);
        var dupCriado = await Orquestrador().ProcessarAsync(criado);
        await Orquestrador().ProcessarAsync(reservado);
        var dupReservado = await Orquestrador().ProcessarAsync(reservado);

        dupCriado.Tipo.ShouldBe(TipoDeResultado.Duplicada);
        dupCriado.Versao.ShouldBe(1);
        dupCriado.ComandosPublicados.ShouldHaveSingleItem().ShouldBeOfType<ReservarEstoque>();
        dupReservado.Tipo.ShouldBe(TipoDeResultado.Duplicada);
        dupReservado.Versao.ShouldBe(2, "duplicata não grava nada");
        dupReservado.ComandosPublicados.ShouldHaveSingleItem().ShouldBeOfType<AutorizarPagamento>();

        var saga = await SagaAsync();
        saga.Versao.ShouldBe(2);
        saga.Status.ShouldBe(StatusSaga.AguardandoPagamento);

        // Republicou (cobre "gravou e caiu antes de publicar"), mas com os MESMOS ids.
        var publicados = _publicador.DoPedido(_pedido);
        publicados.Count.ShouldBe(4);
        publicados.Select(c => c.MessageId).Distinct().ShouldBe(
            [$"{_pedido:N}:ReservarEstoque", $"{_pedido:N}:AutorizarPagamento"], ignoreOrder: true);
    }

    [Fact]
    public async Task Duplicatas_ConcorrentesDoInicio_UmaIniciaAOutraEReconhecidaComoDuplicada()
    {
        var criado = Msg.PedidoCriado(_pedido);

        var resultados = await Task.WhenAll(
            Task.Run(() => Orquestrador().ProcessarAsync(criado)),
            Task.Run(() => Orquestrador().ProcessarAsync(criado)));

        resultados.Select(r => r.Tipo).Order().ShouldBe([TipoDeResultado.Iniciada, TipoDeResultado.Duplicada]);
        (await SagaAsync()).Versao.ShouldBe(1);
    }

    [Fact]
    public async Task ForaDeOrdem_RespostaAntesDeASagaExistir_FalhaComErroTransitorioEDepoisFunciona()
    {
        var reservado = Msg.EstoqueReservado(_pedido);

        var erro = await Should.ThrowAsync<SagaNaoEncontradaException>(() => Orquestrador().ProcessarAsync(reservado));
        new ClassificadorDeErros().Classificar(erro).ShouldBe(TipoDeErro.Transitorio, "o retry atrasado vai trazê-la de volta");
        (await Repositorio.ObterAsync(_pedido)).ShouldBeNull("nada pode ter sido criado");

        await Orquestrador().ProcessarAsync(Msg.PedidoCriado(_pedido));
        var depois = await Orquestrador().ProcessarAsync(reservado); // a "retentativa"

        depois.Tipo.ShouldBe(TipoDeResultado.Aplicada);
        depois.Status.ShouldBe(StatusSaga.AguardandoPagamento);
    }

    [Fact]
    public async Task ForaDeOrdem_PagamentoAutorizadoDepoisDoTimeout_EstornaENuncaConfirma()
    {
        await LevarAteAguardandoPagamentoAsync();
        _relogio.Advance(TimeSpan.FromSeconds(31));
        await Verificador().VerificarAsync();

        var tardio = await Orquestrador().ProcessarAsync(Msg.PagamentoAutorizado(_pedido));
        tardio.Tipo.ShouldBe(TipoDeResultado.Aplicada);
        tardio.ComandosPublicados.ShouldHaveSingleItem().ShouldBeOfType<EstornarPagamento>();
        tardio.Status.ShouldBe(StatusSaga.Compensando);

        var recusaVelha = await Orquestrador().ProcessarAsync(Msg.PagamentoRecusado(_pedido));
        recusaVelha.Tipo.ShouldBe(TipoDeResultado.Ignorada);
        recusaVelha.ComandosPublicados.ShouldBeEmpty();

        await Orquestrador().ProcessarAsync(Msg.EstoqueLiberado(_pedido));
        var saga = await SagaAsync();
        saga.Status.ShouldBe(StatusSaga.Cancelada);
        saga.Passos.HasFlag(PassosDaSaga.PagamentoEstornado).ShouldBeTrue();
        _publicador.DoPedido(_pedido).OfType<ConfirmarPedido>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Concorrencia_TimeoutEPagamentoAoMesmoTempo_OPerdedorReavaliaNoEstadoNovo()
    {
        await LevarAteAguardandoPagamentoAsync(); // versão 2
        _relogio.Advance(TimeSpan.FromSeconds(31));

        // Instância A: vai tratar PagamentoAutorizado, mas para logo depois de LER a versão 2.
        var pausado = new RepositorioComPausa(Repositorio);
        var instanciaA = Orquestrador(pausado).ProcessarAsync(Msg.PagamentoAutorizado(_pedido));
        await pausado.PrimeiraLeituraFeita.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Instância B: o verificador de prazos compensa enquanto A está parada (versão 2 → 3).
        (await Verificador().VerificarAsync()).ShouldBe(1);

        // A continua: tenta concluir sobre a versão 2, perde, recarrega e reavalia.
        pausado.Liberar.SetResult();
        var resultadoA = await instanciaA.WaitAsync(TimeSpan.FromSeconds(10));

        pausado.Conflitos.ShouldBe(1);
        pausado.Leituras.ShouldBe(2, "leu, conflitou, leu de novo");
        resultadoA.Tipo.ShouldBe(TipoDeResultado.Aplicada);
        resultadoA.ComandosPublicados.ShouldHaveSingleItem().ShouldBeOfType<EstornarPagamento>();

        var saga = await SagaAsync();
        saga.Status.ShouldBe(StatusSaga.Compensando);
        saga.Versao.ShouldBe(4);
        _publicador.DoPedido(_pedido).OfType<ConfirmarPedido>().ShouldBeEmpty("confirmar um pedido que o timeout cancelou seria o bug");
    }
}
