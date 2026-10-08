using F6M05.Sagas.Saga;
using F6M05.Tests.Infra;

namespace F6M05.Tests.Parte2_Saga;

/// <summary>Passo 5 — a máquina de estados da saga, pura (sem banco, sem broker).</summary>
public sealed class SagaPedidoTests
{
    private static readonly OpcoesDaSaga Opcoes = new() { PrazoDoPagamento = TimeSpan.FromSeconds(30) };
    private static readonly DateTimeOffset T0 = Msg.Inicio;
    private readonly Guid _pedido = Guid.NewGuid();

    private SagaPedido NovaSaga(decimal valor = 150m) => SagaPedido.Iniciar(Msg.PedidoCriado(_pedido, valor), T0).Saga;

    private SagaPedido AguardandoPagamento()
    {
        var saga = NovaSaga();
        saga.Aplicar(Msg.EstoqueReservado(_pedido), T0.AddSeconds(1), Opcoes).ShouldNotBeNull();
        return saga;
    }

    [Fact]
    public void Iniciar_PedidoCriado_AguardaEstoqueEPedeReserva()
    {
        var evento = Msg.PedidoCriado(_pedido, 150m);

        var (saga, comandos) = SagaPedido.Iniciar(evento, T0);

        saga.PedidoId.ShouldBe(_pedido);
        saga.Status.ShouldBe(StatusSaga.AguardandoEstoque);
        saga.Passos.ShouldBe(PassosDaSaga.Nenhum);
        saga.Versao.ShouldBe(0);
        saga.Valor.ShouldBe(150m);
        saga.CriadaEm.ShouldBe(T0);
        saga.JaProcessou(evento.MessageId).ShouldBeTrue();
        var reservar = comandos.ShouldHaveSingleItem().ShouldBeOfType<ReservarEstoque>();
        reservar.MessageId.ShouldBe($"{_pedido:N}:ReservarEstoque");
        reservar.Itens.ShouldBe(evento.Itens);
    }

    [Fact]
    public void Aplicar_CaminhoFeliz_ReservaAutorizaEConfirma()
    {
        var saga = NovaSaga(150m);

        var aposEstoque = saga.Aplicar(Msg.EstoqueReservado(_pedido), T0.AddSeconds(1), Opcoes);
        aposEstoque.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<AutorizarPagamento>().Valor.ShouldBe(150m);
        saga.Status.ShouldBe(StatusSaga.AguardandoPagamento);
        saga.PrazoPagamentoEm.ShouldBe(T0.AddSeconds(31), "prazo = agora + PrazoDoPagamento");

        var aposPagamento = saga.Aplicar(Msg.PagamentoAutorizado(_pedido), T0.AddSeconds(2), Opcoes);
        aposPagamento.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<ConfirmarPedido>();

        saga.Status.ShouldBe(StatusSaga.Concluida);
        saga.Terminada.ShouldBeTrue();
        saga.Passos.ShouldBe(PassosDaSaga.EstoqueReservado | PassosDaSaga.PagamentoAutorizado | PassosDaSaga.PedidoConfirmado);
        saga.AutorizacaoId.ShouldBe($"aut-{_pedido:N}");
        saga.PrazoPagamentoEm.ShouldBeNull("sem pagamento pendente, sem prazo");
        saga.AtualizadaEm.ShouldBe(T0.AddSeconds(2));
        saga.MensagensProcessadas.Count.ShouldBe(3);
    }

    [Fact]
    public void Aplicar_EstoqueIndisponivel_CancelaSemCompensar()
    {
        var saga = NovaSaga();

        var comandos = saga.Aplicar(Msg.EstoqueIndisponivel(_pedido), T0.AddSeconds(1), Opcoes);

        comandos.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<CancelarPedido>()
            .Motivo.ShouldBe(MotivosDeCancelamento.EstoqueIndisponivel("sem saldo"));
        saga.Status.ShouldBe(StatusSaga.Cancelada);
        saga.Passos.HasFlag(PassosDaSaga.EstoqueLiberado).ShouldBeFalse("nada foi reservado: não há o que compensar");
    }

    [Fact]
    public void Aplicar_PagamentoRecusado_CompensaLiberandoEstoqueAntesDeCancelar()
    {
        var saga = AguardandoPagamento();

        var compensacao = saga.Aplicar(Msg.PagamentoRecusado(_pedido), T0.AddSeconds(2), Opcoes);
        compensacao.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<LiberarEstoque>();
        saga.Status.ShouldBe(StatusSaga.Compensando);
        saga.MotivoCancelamento.ShouldBe(MotivosDeCancelamento.PagamentoRecusado("cartão sem limite"));
        saga.PrazoPagamentoEm.ShouldBeNull();

        var cancelamento = saga.Aplicar(Msg.EstoqueLiberado(_pedido), T0.AddSeconds(3), Opcoes);
        cancelamento.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<CancelarPedido>()
            .Motivo.ShouldBe(MotivosDeCancelamento.PagamentoRecusado("cartão sem limite"));
        saga.Status.ShouldBe(StatusSaga.Cancelada);
        saga.Passos.ShouldBe(PassosDaSaga.EstoqueReservado | PassosDaSaga.EstoqueLiberado | PassosDaSaga.PedidoCancelado);
    }

    [Fact]
    public void Aplicar_PrazoExpirado_SoCompensaQuandoOPrazoVenceu()
    {
        var saga = AguardandoPagamento(); // prazo = T0 + 31 s

        saga.Aplicar(Msg.PrazoExpirado(_pedido), T0.AddSeconds(30), Opcoes)
            .ShouldBeNull("um aviso de prazo adiantado (relógio torto, mensagem velha) não pode cancelar o pedido");
        saga.Status.ShouldBe(StatusSaga.AguardandoPagamento);

        var comandos = saga.Aplicar(Msg.PrazoExpirado(_pedido), T0.AddSeconds(31), Opcoes);
        comandos.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<LiberarEstoque>();
        saga.Status.ShouldBe(StatusSaga.Compensando);
        saga.MotivoCancelamento.ShouldBe(MotivosDeCancelamento.PrazoDoPagamentoExpirado);
    }

    [Fact]
    public void Aplicar_MensagemForaDoEstado_EIgnoradaSemMudarNada()
    {
        var saga = NovaSaga();
        var atualizadaEm = saga.AtualizadaEm;

        // Pagamento respondendo antes de a saga pedir; estoque liberado sem compensação em curso.
        saga.Aplicar(Msg.PagamentoRecusado(_pedido), T0.AddSeconds(1), Opcoes).ShouldBeNull();
        saga.Aplicar(Msg.EstoqueLiberado(_pedido), T0.AddSeconds(1), Opcoes).ShouldBeNull();

        saga.Status.ShouldBe(StatusSaga.AguardandoEstoque);
        saga.AtualizadaEm.ShouldBe(atualizadaEm);
        saga.MensagensProcessadas.Count.ShouldBe(1, "mensagem ignorada não é registrada");

        // Depois de concluída, uma segunda reserva (outro MessageId) também é ignorada.
        var concluida = AguardandoPagamento();
        concluida.Aplicar(Msg.PagamentoAutorizado(_pedido), T0.AddSeconds(2), Opcoes);
        concluida.Aplicar(Msg.EstoqueReservado(_pedido, sufixo: "2"), T0.AddSeconds(3), Opcoes).ShouldBeNull();
        concluida.Status.ShouldBe(StatusSaga.Concluida);
    }

    [Fact]
    public void Aplicar_PagamentoAutorizadoDepoisDoTimeout_EstornaEmVezDeConfirmar()
    {
        var saga = AguardandoPagamento();
        saga.Aplicar(Msg.PrazoExpirado(_pedido), T0.AddSeconds(40), Opcoes);

        var comandos = saga.Aplicar(Msg.PagamentoAutorizado(_pedido), T0.AddSeconds(41), Opcoes);

        var estorno = comandos.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBeOfType<EstornarPagamento>();
        estorno.AutorizacaoId.ShouldBe($"aut-{_pedido:N}");
        saga.Status.ShouldBe(StatusSaga.Compensando, "a compensação do estoque continua");
        saga.Passos.HasFlag(PassosDaSaga.PagamentoEstornado).ShouldBeTrue();
        saga.Passos.HasFlag(PassosDaSaga.PedidoConfirmado).ShouldBeFalse();

        // A mesma autorização tardia, reentregue com outro id, não gera segundo estorno.
        saga.Aplicar(Msg.PagamentoAutorizado(_pedido) with { MessageId = "outra-entrega" }, T0.AddSeconds(42), Opcoes).ShouldBeNull();
    }

    [Fact]
    public void ComandosDoEstadoAtual_RepeteOsMesmosMessageIds()
    {
        var saga = AguardandoPagamento();
        var original = saga.ComandosDoEstadoAtual().ShouldHaveSingleItem().ShouldBeOfType<AutorizarPagamento>();

        original.MessageId.ShouldBe($"{_pedido:N}:AutorizarPagamento");
        saga.ComandosDoEstadoAtual().ShouldHaveSingleItem().ShouldBe(original, "mesmo comando, mesmo id: o participante deduplica");

        saga.Aplicar(Msg.PrazoExpirado(_pedido), T0.AddSeconds(40), Opcoes);
        saga.Aplicar(Msg.PagamentoAutorizado(_pedido), T0.AddSeconds(41), Opcoes);
        saga.Aplicar(Msg.EstoqueLiberado(_pedido), T0.AddSeconds(42), Opcoes);

        saga.Status.ShouldBe(StatusSaga.Cancelada);
        saga.ComandosDoEstadoAtual().Select(c => c.MessageId).ShouldBe(
            [$"{_pedido:N}:CancelarPedido", $"{_pedido:N}:EstornarPagamento"], ignoreOrder: true);
    }
}
