using F3M03.Transacoes.EfCore;
using F3M03.Transacoes.Estoque;
using F3M03.Transacoes.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F3M03.Transacoes.Tests;

/// <summary>Passos 9 e 10: concorrência otimista e transação explícita no EF Core.</summary>
public sealed class EfCoreTests(BancoFixture fixture) : BancoTestBase(fixture)
{
    // ------------------------------------------------------------ Passo 9: rowversion no EF Core

    [Fact]
    public async Task ReservarOtimistaEf_SemConcorrencia_DaBaixa()
    {
        var resultado = await new EstoqueComEfCore(Cs).ReservarOtimistaAsync(2, 4);

        resultado.ShouldBe(ResultadoReserva.Reservado);
        (await EstoqueAsync(2)).ShouldBe(6);
    }

    [Theory]
    [InlineData(1, ResultadoReserva.Conflito, 7, 1)]
    [InlineData(3, ResultadoReserva.Reservado, 5, 2)]
    public async Task ReservarOtimistaEf_Intercalado_DetectaConflitoEComRetryRecarregaDoBanco(
        int maxTentativasDeB, ResultadoReserva esperadoB, int estoqueFinal, int chamadasDoGanchoB)
    {
        var servico = new EstoqueComEfCore(Cs);
        using var pausaA = new PontoDeParada("A");
        using var pausaB = new PontoDeParada("B");
        Task<ResultadoReserva>? a = null, b = null;

        try
        {
            a = servico.ReservarOtimistaAsync(1, 3, maxTentativas: 1, pausaA.Gancho);
            await pausaA.EsperarChegadaAsync(a);
            b = servico.ReservarOtimistaAsync(1, 2, maxTentativasDeB, pausaB.Gancho);
            await pausaB.EsperarChegadaAsync(b);

            pausaA.Liberar();
            (await NoMaximoAsync(a, "Reserva A")).ShouldBe(ResultadoReserva.Reservado);
            pausaB.Liberar();
            (await NoMaximoAsync(b, "Reserva B")).ShouldBe(esperadoB,
                "se B ficou em Conflito mesmo com retry, ela não recarregou do banco (consultar de novo um " +
                "DbContext que já rastreia a entidade devolve a instância da memória, com a Versao velha)");

            pausaB.Chamadas.ShouldBe(chamadasDoGanchoB);
            (await EstoqueAsync(1)).ShouldBe(estoqueFinal);
        }
        finally
        {
            pausaA.Liberar();
            pausaB.Liberar();
            await DrenarAsync(a, b);
        }
    }

    // ------------------------------------------------------------ Passo 10: transação explícita

    [Fact]
    public async Task CriarPedido_ComEstoque_GravaPedidoEBaixaJuntos()
    {
        var pedidoId = await new EstoqueComEfCore(Cs).CriarPedidoAsync(clienteId: 2, produtoId: 3, quantidade: 4);

        pedidoId.ShouldNotBeNull();
        (await EstoqueAsync(3)).ShouldBe(6);
        (await EscalarInfraAsync<int>($"SELECT Quantidade FROM dbo.Pedidos WHERE Id = {pedidoId};")).ShouldBe(4);
    }

    [Fact]
    public async Task CriarPedido_SemEstoque_NaoGravaNada()
    {
        var pedidoId = await new EstoqueComEfCore(Cs).CriarPedidoAsync(clienteId: 2, produtoId: 3, quantidade: 11);

        pedidoId.ShouldBeNull();
        (await EstoqueAsync(3)).ShouldBe(10);
        (await EscalarInfraAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos;")).ShouldBe(3);
    }

    [Fact]
    public async Task CriarPedido_FalhaAoGravarOPedido_BaixaDeEstoqueTambemEDesfeita()
    {
        // Cliente 999 não existe: o INSERT do pedido viola a FK DEPOIS que a baixa já foi executada.
        await Should.ThrowAsync<DbUpdateException>(
            () => new EstoqueComEfCore(Cs).CriarPedidoAsync(clienteId: 999, produtoId: 3, quantidade: 4));

        (await EstoqueAsync(3)).ShouldBe(10,
            "ExecuteUpdate fez autocommit: sem BeginTransaction, a baixa ficou gravada sem pedido (estoque 'sumiu')");
        (await EscalarInfraAsync<int>("SELECT COUNT(*) FROM dbo.Pedidos;")).ShouldBe(3);
    }
}
