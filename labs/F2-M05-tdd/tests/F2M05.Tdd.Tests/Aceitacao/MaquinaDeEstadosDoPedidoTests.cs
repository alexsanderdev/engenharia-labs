using Microsoft.Extensions.Time.Testing;

namespace F2M05.Tdd.Tests.Aceitacao;

/// <summary>
/// Testes de ACEITAÇÃO: definem o "pronto" do lab, vistos de fora (como o resto do OrderFlow usa o Pedido).
/// Não altere estes testes. Eles NÃO substituem os seus testes unitários em MeusTestes/:
/// são o alvo; os seus testes são o caminho, escritos um de cada vez, em ciclos red-green-refactor.
/// </summary>
public sealed class MaquinaDeEstadosDoPedidoTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 8, 14, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _relogio = new(Inicio);

    [Fact]
    public void NovoPedido_ComecaEmCreated_SemHistorico()
    {
        var pedido = new Pedido(_relogio);

        pedido.Status.ShouldBe(StatusPedido.Created);
        pedido.Historico.ShouldBeEmpty();
    }

    [Fact]
    public void NovoPedido_RegistraADataDeCriacaoDoRelogio()
    {
        var pedido = new Pedido(_relogio);

        pedido.CriadoEm.ShouldBe(Inicio);
    }

    [Fact]
    public void FluxoCompleto_CreatedConfirmedCompleted_RegistraCadaTransicaoComSuaData()
    {
        var pedido = new Pedido(_relogio);

        _relogio.Advance(TimeSpan.FromMinutes(5));
        pedido.Confirmar();
        _relogio.Advance(TimeSpan.FromHours(2));
        pedido.Concluir();

        pedido.Status.ShouldBe(StatusPedido.Completed);
        pedido.Historico.ShouldBe(
        [
            new TransicaoDeStatus(StatusPedido.Created, StatusPedido.Confirmed, Inicio.AddMinutes(5)),
            new TransicaoDeStatus(StatusPedido.Confirmed, StatusPedido.Completed, Inicio.AddMinutes(5).AddHours(2)),
        ]);
    }

    [Fact]
    public void PedidoCriado_AoCancelar_FicaCancelledERegistraOMotivo()
    {
        var pedido = new Pedido(_relogio);
        _relogio.Advance(TimeSpan.FromMinutes(1));

        pedido.Cancelar("  Cliente desistiu  ");

        pedido.Status.ShouldBe(StatusPedido.Cancelled);
        pedido.Historico.ShouldHaveSingleItem()
            .ShouldBe(new TransicaoDeStatus(StatusPedido.Created, StatusPedido.Cancelled, Inicio.AddMinutes(1), "Cliente desistiu"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancelar_SemMotivo_LancaArgumentExceptionENaoMudaNada(string? motivo)
    {
        var pedido = new Pedido(_relogio);

        Should.Throw<ArgumentException>(() => pedido.Cancelar(motivo!));

        pedido.Status.ShouldBe(StatusPedido.Created);
        pedido.Historico.ShouldBeEmpty();
    }

    [Fact]
    public void PedidoConcluido_AoCancelar_LancaENaoMudaNada()
    {
        var pedido = new Pedido(_relogio);
        pedido.Confirmar();
        pedido.Concluir();

        var ex = Should.Throw<TransicaoInvalidaException>(() => pedido.Cancelar("Arrependimento"));

        ex.De.ShouldBe(StatusPedido.Completed);
        ex.Para.ShouldBe(StatusPedido.Cancelled);
        pedido.Status.ShouldBe(StatusPedido.Completed);
        pedido.Historico.Count.ShouldBe(2);
    }

    [Fact]
    public void PedidoCriado_AoConcluirSemConfirmar_Lanca()
    {
        var pedido = new Pedido(_relogio);

        Should.Throw<TransicaoInvalidaException>(pedido.Concluir);

        pedido.Status.ShouldBe(StatusPedido.Created);
        pedido.Historico.ShouldBeEmpty();
    }

    [Fact]
    public void PedidoCancelado_AoConfirmar_Lanca()
    {
        var pedido = new Pedido(_relogio);
        pedido.Cancelar("Sem estoque");

        Should.Throw<TransicaoInvalidaException>(pedido.Confirmar);

        pedido.Status.ShouldBe(StatusPedido.Cancelled);
    }

    [Fact]
    public void PedidoConfirmado_AoConfirmarDeNovo_Lanca()
    {
        var pedido = new Pedido(_relogio);
        pedido.Confirmar();

        Should.Throw<TransicaoInvalidaException>(pedido.Confirmar);

        pedido.Historico.Count.ShouldBe(1);
    }

    [Fact]
    public void PedidoConfirmado_AoCancelar_Lanca()
    {
        // Regra atual do negócio: depois de confirmado, o pedido só pode ser concluído.
        var pedido = new Pedido(_relogio);
        pedido.Confirmar();

        Should.Throw<TransicaoInvalidaException>(() => pedido.Cancelar("Mudei de ideia"));

        pedido.Status.ShouldBe(StatusPedido.Confirmed);
    }

    [Fact]
    public void TransicaoInvalida_MensagemDizDeOndeParaOnde()
    {
        var pedido = new Pedido(_relogio);

        var ex = Should.Throw<TransicaoInvalidaException>(pedido.Concluir);

        ex.Message.ShouldContain("Created");
        ex.Message.ShouldContain("Completed");
    }

    [Theory]
    [InlineData(StatusPedido.Created, true)]
    [InlineData(StatusPedido.Confirmed, false)]
    [InlineData(StatusPedido.Completed, false)]
    [InlineData(StatusPedido.Cancelled, false)]
    public void PodeCancelar_RefleteOStatusAtual(StatusPedido status, bool esperado)
    {
        var pedido = PedidoNoStatus(status);

        pedido.PodeCancelar.ShouldBe(esperado);
    }

    private Pedido PedidoNoStatus(StatusPedido status)
    {
        var pedido = new Pedido(_relogio);
        switch (status)
        {
            case StatusPedido.Confirmed:
                pedido.Confirmar();
                break;
            case StatusPedido.Completed:
                pedido.Confirmar();
                pedido.Concluir();
                break;
            case StatusPedido.Cancelled:
                pedido.Cancelar("teste");
                break;
        }
        return pedido;
    }
}
