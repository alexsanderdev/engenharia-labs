using F6M05.Sagas.Saga;
using F6M05.Tests.Infra;

namespace F6M05.Tests.Parte2_Saga;

/// <summary>Passo 6 — estado da saga no SQL Server com concorrência otimista.</summary>
public sealed class RepositorioDeSagasTests(InfraFixture infra) : SqlTestBase(infra)
{
    private static readonly OpcoesDaSaga Opcoes = new() { PrazoDoPagamento = TimeSpan.FromSeconds(30) };
    private static readonly DateTimeOffset T0 = Msg.Inicio;
    private RepositorioDeSagasSql Repositorio => new(Infra.Sql);

    [Fact]
    public async Task InserirEObter_DevolveOMesmoEstadoNaVersao1()
    {
        var pedido = Guid.NewGuid();
        var evento = Msg.PedidoCriado(pedido, 321.45m);
        var (saga, _) = SagaPedido.Iniciar(evento, T0);

        await Repositorio.InserirAsync(saga, evento.MessageId);

        saga.Versao.ShouldBe(1);
        var lida = (await Repositorio.ObterAsync(pedido)).ShouldNotBeNull();
        lida.Versao.ShouldBe(1);
        lida.Status.ShouldBe(StatusSaga.AguardandoEstoque);
        lida.ClienteId.ShouldBe(evento.ClienteId);
        lida.Valor.ShouldBe(321.45m);
        lida.Itens.ShouldBe(evento.Itens);
        lida.CriadaEm.ShouldBe(T0);
        lida.JaProcessou(evento.MessageId).ShouldBeTrue();
        (await Repositorio.ObterAsync(Guid.NewGuid())).ShouldBeNull();
    }

    [Fact]
    public async Task Inserir_SagaQueJaExiste_LancaConflito()
    {
        var evento = Msg.PedidoCriado(Guid.NewGuid());
        await Repositorio.InserirAsync(SagaPedido.Iniciar(evento, T0).Saga, evento.MessageId);

        await Should.ThrowAsync<ConflitoDeConcorrenciaException>(
            () => Repositorio.InserirAsync(SagaPedido.Iniciar(evento, T0).Saga, "outra-instancia"));
    }

    [Fact]
    public async Task Atualizar_GravaIncrementaVersaoERegistraMensagem()
    {
        var pedido = Guid.NewGuid();
        var evento = Msg.PedidoCriado(pedido);
        await Repositorio.InserirAsync(SagaPedido.Iniciar(evento, T0).Saga, evento.MessageId);

        var saga = (await Repositorio.ObterAsync(pedido)).ShouldNotBeNull();
        var reservado = Msg.EstoqueReservado(pedido);
        saga.Aplicar(reservado, T0.AddSeconds(1), Opcoes);
        await Repositorio.AtualizarAsync(saga, reservado.MessageId);

        saga.Versao.ShouldBe(2);
        var lida = (await Repositorio.ObterAsync(pedido)).ShouldNotBeNull();
        lida.Versao.ShouldBe(2);
        lida.Status.ShouldBe(StatusSaga.AguardandoPagamento);
        lida.Passos.ShouldBe(PassosDaSaga.EstoqueReservado);
        lida.PrazoPagamentoEm.ShouldBe(T0.AddSeconds(31));
        lida.AtualizadaEm.ShouldBe(T0.AddSeconds(1));
        lida.MensagensProcessadas.ShouldBe([evento.MessageId, reservado.MessageId], ignoreOrder: true);
    }

    [Fact]
    public async Task Atualizar_ComVersaoDesatualizada_LancaConflitoENaoGravaNada()
    {
        var pedido = Guid.NewGuid();
        var evento = Msg.PedidoCriado(pedido);
        await Repositorio.InserirAsync(SagaPedido.Iniciar(evento, T0).Saga, evento.MessageId);

        // Duas instâncias leem a versão 1.
        var instanciaA = (await Repositorio.ObterAsync(pedido)).ShouldNotBeNull();
        var instanciaB = (await Repositorio.ObterAsync(pedido)).ShouldNotBeNull();

        var reservado = Msg.EstoqueReservado(pedido);
        instanciaA.Aplicar(reservado, T0.AddSeconds(1), Opcoes);
        await Repositorio.AtualizarAsync(instanciaA, reservado.MessageId); // 1 → 2

        var indisponivel = Msg.EstoqueIndisponivel(pedido);
        instanciaB.Aplicar(indisponivel, T0.AddSeconds(1), Opcoes);
        var conflito = await Should.ThrowAsync<ConflitoDeConcorrenciaException>(
            () => Repositorio.AtualizarAsync(instanciaB, indisponivel.MessageId));
        conflito.VersaoEsperada.ShouldBe(1);

        var lida = (await Repositorio.ObterAsync(pedido)).ShouldNotBeNull();
        lida.Versao.ShouldBe(2);
        lida.Status.ShouldBe(StatusSaga.AguardandoPagamento, "a escrita de B não pode ter sobrescrito a de A (lost update)");
        lida.JaProcessou(indisponivel.MessageId).ShouldBeFalse("a mensagem de B também não foi registrada (mesma transação)");
    }

    [Fact]
    public async Task ListarComPrazoVencido_SoAguardandoPagamentoComPrazoNoPassado()
    {
        var vencida = await CriarAguardandoPagamentoAsync(reservadoEm: T0);                 // prazo T0+30
        var noFuturo = await CriarAguardandoPagamentoAsync(reservadoEm: T0.AddSeconds(30));  // prazo T0+60
        var aguardandoEstoque = Msg.PedidoCriado(Guid.NewGuid());
        await Repositorio.InserirAsync(SagaPedido.Iniciar(aguardandoEstoque, T0).Saga, aguardandoEstoque.MessageId);

        var ids = await Repositorio.ListarComPrazoVencidoAsync(T0.AddSeconds(45));

        ids.ShouldBe([vencida]);
        (await Repositorio.ListarComPrazoVencidoAsync(T0.AddSeconds(60))).ShouldBe([vencida, noFuturo], ignoreOrder: true);
    }

    private async Task<Guid> CriarAguardandoPagamentoAsync(DateTimeOffset reservadoEm)
    {
        var pedido = Guid.NewGuid();
        var evento = Msg.PedidoCriado(pedido);
        var (saga, _) = SagaPedido.Iniciar(evento, T0);
        await Repositorio.InserirAsync(saga, evento.MessageId);
        var reservado = Msg.EstoqueReservado(pedido);
        saga.Aplicar(reservado, reservadoEm, Opcoes);
        await Repositorio.AtualizarAsync(saga, reservado.MessageId);
        return pedido;
    }
}
