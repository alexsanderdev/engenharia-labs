using F3M03.Transacoes.Deadlocks;
using F3M03.Transacoes.Tests.Infra;

namespace F3M03.Transacoes.Tests;

/// <summary>Passo 6: corrigir o deadlock na causa, com ordem de acesso consistente.</summary>
public sealed class OrdemDeAcessoTests(BancoFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task MoverEmOrdemConsistente_CaminhoFeliz_NosDoisSentidos()
    {
        var transferencia = new TransferenciaDeEstoque(Cs);

        (await transferencia.MoverEmOrdemConsistenteAsync(1, 2, 4)).ShouldBeTrue();
        (await transferencia.MoverEmOrdemConsistenteAsync(3, 1, 5)).ShouldBeTrue();

        (await EstoqueAsync(1)).ShouldBe(10 - 4 + 5);
        (await EstoqueAsync(2)).ShouldBe(14);
        (await EstoqueAsync(3)).ShouldBe(5);
    }

    [Fact]
    public async Task MoverEmOrdemConsistente_OrigemSemEstoque_NaoMexeEmNenhumDosDois()
    {
        // 3 → 1: na ordem consistente, o destino (Id 1) é atualizado ANTES de descobrir que a
        // origem (Id 3) não tem 50 unidades. O "+50" no destino precisa ser desfeito.
        var ok = await new TransferenciaDeEstoque(Cs).MoverEmOrdemConsistenteAsync(3, 1, 50);

        ok.ShouldBeFalse();
        (await EstoqueAsync(1)).ShouldBe(10);
        (await EstoqueAsync(3)).ShouldBe(10);
    }

    [Fact]
    public async Task MoverEmOrdemConsistente_TransferenciasCruzadas_SegundaEsperaENaoHaDeadlock()
    {
        using var pausaA = new PontoDeParada("A");
        using var pausaB = new PontoDeParada("B");
        Task<bool>? a = null, b = null;

        try
        {
            // Mesma orquestração que gera deadlock na versão "na ordem do pedido".
            a = new TransferenciaDeEstoque(Cs).MoverEmOrdemConsistenteAsync(1, 2, 3, pausaA.Gancho);
            await pausaA.EsperarChegadaAsync(a); // A já atualizou o produto 1 (o menor Id)

            b = new TransferenciaDeEstoque(Cs, PrioridadeDeDeadlock.Baixa).MoverEmOrdemConsistenteAsync(2, 1, 4, pausaB.Gancho);

            // B também começa pelo produto 1, que está com A: B espera ANTES de travar qualquer coisa.
            await GarantirQueFicouBloqueadaAsync(pausaB, b,
                "B travou o produto 2 primeiro (chegou ao ponto de parada sem esperar A): " +
                "as duas transferências precisam atualizar os produtos na MESMA ordem (menor Id primeiro)");

            pausaA.Liberar();
            (await NoMaximoAsync(a, "Transferência A")).ShouldBeTrue();

            await pausaB.EsperarChegadaAsync(b);
            pausaB.Liberar();
            (await NoMaximoAsync(b, "Transferência B")).ShouldBeTrue("sem ciclo, ninguém vira vítima de deadlock");

            (await EstoqueAsync(1)).ShouldBe(10 - 3 + 4);
            (await EstoqueAsync(2)).ShouldBe(10 + 3 - 4);
        }
        finally
        {
            pausaA.Liberar();
            pausaB.Liberar();
            await DrenarAsync(a, b);
        }
    }
}
