using F3M03.Transacoes.Deadlocks;
using F3M03.Transacoes.Estoque;
using F3M03.Transacoes.Tests.Infra;

namespace F3M03.Transacoes.Tests.Demonstracoes;

/// <summary>
/// DEMONSTRAÇÕES (já vêm verdes): lost update no estoque, deadlock clássico e o comportamento
/// do LOCK_TIMEOUT. São os problemas que os testes vermelhos do lab pedem para você corrigir.
/// </summary>
public sealed class LostUpdateEDeadlockDemoTests(BancoFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task ReservarIngenuo_DuasReservasIntercaladas_UmaBaixaSePerde()
    {
        var servico = new ReservaDeEstoque(Cs);
        using var pausaA = new PontoDeParada("A");
        using var pausaB = new PontoDeParada("B");

        // A lê 10 e para.  B lê 10 e para.  A grava 10-3=7.  B grava 10-2=8.
        var a = servico.ReservarIngenuoAsync(1, 3, pausaA.Gancho);
        await pausaA.EsperarChegadaAsync(a);
        var b = servico.ReservarIngenuoAsync(1, 2, pausaB.Gancho);
        await pausaB.EsperarChegadaAsync(b);

        pausaA.Liberar();
        (await NoMaximoAsync(a, "Reserva A")).ShouldBe(ResultadoReserva.Reservado);
        pausaB.Liberar();
        (await NoMaximoAsync(b, "Reserva B")).ShouldBe(ResultadoReserva.Reservado);

        // As duas "deram certo", vendemos 5 unidades... e o banco diz que só saíram 2.
        (await EstoqueAsync(1)).ShouldBe(8, "o certo seria 5: a baixa de A foi sobrescrita (lost update)");
    }

    [Fact]
    public async Task Deadlock_OrdemDeAcessoInvertida_VitimaDeMenorPrioridadeRecebe1205()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");
        await b.ExecutarAsync("SET DEADLOCK_PRIORITY LOW;"); // torna a escolha da vítima determinística

        await a.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Estoque = Estoque - 1 WHERE Id = 1;"); // A: X em 1
        await b.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Estoque = Estoque - 1 WHERE Id = 2;"); // B: X em 2

        var aPede2 = a.ExecutarAsync("UPDATE dbo.Produtos SET Estoque = Estoque + 1 WHERE Id = 2;"); // A espera B
        await EsperarBloqueioDaSessaoAsync(a.Spid);

        // B pede 1 (que está com A): ciclo A -> B -> A. O lock monitor detecta (em até ~5 s),
        // escolhe a vítima de MENOR DEADLOCK_PRIORITY, desfaz a transação dela e lança 1205.
        var erro = await NoMaximoAsync(b.FalharAsync("UPDATE dbo.Produtos SET Estoque = Estoque + 1 WHERE Id = 1;"), "B");

        await NoMaximoAsync(aPede2, "A"); // a vítima soltou os locks; A segue
        await a.ExecutarAsync("COMMIT;");

        erro.Number.ShouldBe(1205);
        erro.Message.ShouldContain("deadlock");
        (await b.TranCountAsync()).ShouldBe(0, "a transação da vítima foi desfeita por inteiro");
        (await EstoqueAsync(1)).ShouldBe(9);
        (await EstoqueAsync(2)).ShouldBe(11, "só a transferência de A valeu; a de B sumiu e precisa ser refeita (retry)");
    }

    [Fact]
    public async Task LockTimeout_EstouraComErro1222_MasNaoDesfazATransacao()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");

        await a.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Estoque = 0 WHERE Id = 1;");

        await b.ExecutarAsync("SET LOCK_TIMEOUT 200; BEGIN TRAN; UPDATE dbo.Produtos SET Estoque = 99 WHERE Id = 2;");
        var erro = await b.FalharAsync("SELECT Estoque FROM dbo.Produtos WHERE Id = 1;");

        erro.Number.ShouldBe(1222); // "Lock request time out period exceeded"
        // ARMADILHA: o erro aborta só o COMANDO. A transação de B continua aberta, segurando o
        // X lock no produto 2. Se o código só logar o erro e devolver a conexão, o lock fica preso.
        (await b.TranCountAsync()).ShouldBe(1);

        await b.ExecutarAsync("ROLLBACK;");
        await a.ExecutarAsync("ROLLBACK;");
        (await EstoqueAsync(2)).ShouldBe(10);
    }

    [Fact]
    public async Task LockTimeout_ComXactAbortOn_DesfazATransacaoInteira()
    {
        var a = await AbrirSessaoAsync("A");
        var b = await AbrirSessaoAsync("B");

        await a.ExecutarAsync("BEGIN TRAN; UPDATE dbo.Produtos SET Estoque = 0 WHERE Id = 1;");

        // XACT_ABORT ON: qualquer erro de execução desfaz a transação inteira. É o que você quer
        // em quase todo código de escrita (e o que o EF Core faria por você chamando Rollback).
        await b.ExecutarAsync("SET XACT_ABORT ON; SET LOCK_TIMEOUT 200; BEGIN TRAN; UPDATE dbo.Produtos SET Estoque = 99 WHERE Id = 2;");
        var erro = await b.FalharAsync("SELECT Estoque FROM dbo.Produtos WHERE Id = 1;");

        erro.Number.ShouldBe(1222);
        (await b.TranCountAsync()).ShouldBe(0);
        await a.ExecutarAsync("ROLLBACK;");
        (await EstoqueAsync(2)).ShouldBe(10);
    }

    [Fact]
    public async Task TransferenciaNaOrdemDoPedido_CaminhoFeliz_MoveEstoque()
    {
        // Sanidade da versão pronta (sem concorrência ela funciona; o problema é o deadlock).
        var ok = await new TransferenciaDeEstoque(Cs).MoverNaOrdemDoPedidoAsync(1, 2, 4);

        ok.ShouldBeTrue();
        (await EstoqueAsync(1)).ShouldBe(6);
        (await EstoqueAsync(2)).ShouldBe(14);
    }
}
