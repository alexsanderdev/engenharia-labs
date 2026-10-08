using Microsoft.Extensions.Time.Testing;

namespace F2M05.Tdd.Tests.MeusTestes;

/// <summary>
/// Testes unitários escritos em ciclos red-green-refactor, na ordem do "Roteiro de ciclos" da nota Lab.
/// Cada teste corresponde a um ciclo (e a um commit). O comentário "Ciclo N" ajuda a comparar com o seu histórico.
/// </summary>
public sealed class PedidoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 8, 14, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _relogio = new(Agora);

    private Pedido NovoPedido() => new(_relogio);

    // Ciclo 1 — o teste mais simples possível. Green com "fake it": Status => StatusPedido.Created.
    [Fact]
    public void NovoPedido_ComecaEmCreated()
    {
        var pedido = NovoPedido();

        pedido.Status.ShouldBe(StatusPedido.Created);
    }

    // Ciclo 2 — histórico vazio. Green: Historico => [].
    [Fact]
    public void NovoPedido_TemHistoricoVazio()
    {
        var pedido = NovoPedido();

        pedido.Historico.ShouldBeEmpty();
    }

    // Ciclo 3 — data de criação. Green "fake it" possível: CriadoEm => new DateTimeOffset(2026, 10, 8, ...).
    [Fact]
    public void NovoPedido_GuardaCriadoEmDoRelogio()
    {
        var pedido = NovoPedido();

        pedido.CriadoEm.ShouldBe(Agora);
    }

    // Ciclo 4 — TRIANGULAÇÃO: um segundo exemplo com outra data derruba a constante do ciclo 3
    // e obriga a implementação real (ler o TimeProvider no construtor).
    [Fact]
    public void NovoPedido_EmOutroInstante_GuardaOutroCriadoEm()
    {
        var outroInstante = new DateTimeOffset(2030, 1, 2, 3, 4, 5, TimeSpan.Zero);

        var pedido = new Pedido(new FakeTimeProvider(outroInstante));

        pedido.CriadoEm.ShouldBe(outroInstante);
    }

    // Ciclo 5 — primeira transição. Green: Status vira { get; private set; } e Confirmar() atribui Confirmed.
    [Fact]
    public void Confirmar_PedidoCriado_FicaConfirmed()
    {
        var pedido = NovoPedido();

        pedido.Confirmar();

        pedido.Status.ShouldBe(StatusPedido.Confirmed);
    }

    // Ciclo 6 — histórico com data. Green: lista privada + Add(new TransicaoDeStatus(...)).
    [Fact]
    public void Confirmar_RegistraTransicaoComADataDoRelogio()
    {
        var pedido = NovoPedido();
        _relogio.Advance(TimeSpan.FromMinutes(3));

        pedido.Confirmar();

        pedido.Historico.ShouldHaveSingleItem()
            .ShouldBe(new TransicaoDeStatus(StatusPedido.Created, StatusPedido.Confirmed, Agora.AddMinutes(3)));
    }

    // Ciclo 7 — caminho feliz até o fim.
    [Fact]
    public void Concluir_PedidoConfirmado_FicaCompletedComDuasTransicoes()
    {
        var pedido = NovoPedido();
        pedido.Confirmar();

        pedido.Concluir();

        pedido.Status.ShouldBe(StatusPedido.Completed);
        pedido.Historico.Select(t => t.Para).ShouldBe([StatusPedido.Confirmed, StatusPedido.Completed]);
    }

    // Ciclo 8 — a primeira REGRA. Até aqui Concluir() aceitava tudo; agora: if (Status != Confirmed) throw.
    [Fact]
    public void Concluir_PedidoCriado_LancaTransicaoInvalida()
    {
        var pedido = NovoPedido();

        Should.Throw<TransicaoInvalidaException>(pedido.Concluir);
    }

    // Ciclo 9 — cancelar com motivo.
    [Fact]
    public void Cancelar_PedidoCriado_FicaCancelledComMotivoNoHistorico()
    {
        var pedido = NovoPedido();

        pedido.Cancelar("Cliente desistiu");

        pedido.Status.ShouldBe(StatusPedido.Cancelled);
        pedido.Historico.ShouldHaveSingleItem().Motivo.ShouldBe("Cliente desistiu");
    }

    // Ciclo 10 — motivo obrigatório. Teste parametrizado: três exemplos da mesma regra.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Cancelar_SemMotivo_LancaArgumentException(string? motivo)
    {
        var pedido = NovoPedido();

        Should.Throw<ArgumentException>(() => pedido.Cancelar(motivo!));
    }

    // Ciclo 11 — o motivo é normalizado.
    [Fact]
    public void Cancelar_MotivoComEspacos_GuardaSemEspacosNasPontas()
    {
        var pedido = NovoPedido();

        pedido.Cancelar("  Sem estoque ");

        pedido.Historico[0].Motivo.ShouldBe("Sem estoque");
    }

    // Ciclo 12 — "Completed não cancela".
    [Fact]
    public void Cancelar_PedidoConcluido_LancaTransicaoInvalida()
    {
        var pedido = NovoPedido();
        pedido.Confirmar();
        pedido.Concluir();

        Should.Throw<TransicaoInvalidaException>(() => pedido.Cancelar("Arrependimento"));
    }

    // Ciclo 13 — terceiro "if (Status != X) throw" à vista: hora do REFACTOR que extrai RegrasDeTransicao.
    [Fact]
    public void Confirmar_PedidoCancelado_LancaTransicaoInvalida()
    {
        var pedido = NovoPedido();
        pedido.Cancelar("Sem estoque");

        Should.Throw<TransicaoInvalidaException>(pedido.Confirmar);
    }

    // Ciclo 14 — falha não deixa estado pela metade (protege o refactor do ciclo 13).
    [Fact]
    public void TransicaoInvalida_NaoAlteraStatusNemHistorico()
    {
        var pedido = NovoPedido();
        pedido.Confirmar();

        Should.Throw<TransicaoInvalidaException>(pedido.Confirmar);

        pedido.Status.ShouldBe(StatusPedido.Confirmed);
        pedido.Historico.Count.ShouldBe(1);
    }

    // Ciclo 15 — a exceção conta o que aconteceu.
    [Fact]
    public void TransicaoInvalida_ExcecaoInformaDeEPara()
    {
        var pedido = NovoPedido();

        var ex = Should.Throw<TransicaoInvalidaException>(pedido.Concluir);

        ex.De.ShouldBe(StatusPedido.Created);
        ex.Para.ShouldBe(StatusPedido.Completed);
    }

    // Ciclo 16 — PodeCancelar reaproveita a tabela de transições.
    [Fact]
    public void PodeCancelar_PedidoCriadoVerdadeiro_DepoisDeConfirmadoFalso()
    {
        var pedido = NovoPedido();
        pedido.PodeCancelar.ShouldBeTrue();

        pedido.Confirmar();

        pedido.PodeCancelar.ShouldBeFalse();
    }
}
