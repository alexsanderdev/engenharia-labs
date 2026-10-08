using F3M03.Transacoes.Estoque;
using F3M03.Transacoes.Tests.Infra;

namespace F3M03.Transacoes.Tests;

/// <summary>Passos 1 a 3: as três correções do lost update. Todos os produtos começam com estoque 10.</summary>
public sealed class ReservaDeEstoqueTests(BancoFixture fixture) : BancoTestBase(fixture)
{
    // ------------------------------------------------------------ Passo 1: UPDATE atômico

    [Fact]
    public async Task ReservarAtomico_ComEstoque_DaBaixa()
    {
        var resultado = await new ReservaDeEstoque(Cs).ReservarAtomicoAsync(1, 3);

        resultado.ShouldBe(ResultadoReserva.Reservado);
        (await EstoqueAsync(1)).ShouldBe(7);
    }

    [Fact]
    public async Task ReservarAtomico_SemEstoqueSuficiente_NaoMexeNoEstoque()
    {
        var resultado = await new ReservaDeEstoque(Cs).ReservarAtomicoAsync(1, 11);

        resultado.ShouldBe(ResultadoReserva.EstoqueInsuficiente);
        (await EstoqueAsync(1)).ShouldBe(10);
    }

    [Fact]
    public async Task ReservarAtomico_VinteReservasSimultaneasDeUmaUnidade_ExatamenteDezPassamENuncaFicaNegativo()
    {
        var servico = new ReservaDeEstoque(Cs);

        // Concorrência "de verdade" (sem orquestração): o resultado tem que ser o mesmo em
        // QUALQUER intercalação, por isso este teste é determinístico apesar do paralelismo.
        var resultados = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => Task.Run(() => servico.ReservarAtomicoAsync(1, 1))));

        resultados.Count(r => r == ResultadoReserva.Reservado).ShouldBe(10);
        resultados.Count(r => r == ResultadoReserva.EstoqueInsuficiente).ShouldBe(10);
        (await EstoqueAsync(1)).ShouldBe(0);
    }

    // ------------------------------------------------------------ Passo 2: UPDLOCK, HOLDLOCK

    [Fact]
    public async Task ReservarComUpdLock_DuasReservasIntercaladas_SegundaEsperaENenhumaBaixaSePerde()
    {
        var servico = new ReservaDeEstoque(Cs);
        using var pausaA = new PontoDeParada("A");
        using var pausaB = new PontoDeParada("B");
        Task<ResultadoReserva>? a = null, b = null;

        try
        {
            a = servico.ReservarComUpdLockAsync(1, 3, pausaA.Gancho);
            await pausaA.EsperarChegadaAsync(a); // A leu (com U lock) e está parada

            b = servico.ReservarComUpdLockAsync(1, 2, pausaB.Gancho);

            // B NÃO pode chegar ao ponto de parada: tem que ficar bloqueada na leitura.
            await GarantirQueFicouBloqueadaAsync(pausaB, b,
                "B leu o estoque enquanto A ainda estava no meio da transação: a leitura precisa de WITH (UPDLOCK, HOLDLOCK)");

            pausaA.Liberar();
            (await NoMaximoAsync(a, "Reserva A")).ShouldBe(ResultadoReserva.Reservado);

            await pausaB.EsperarChegadaAsync(b); // agora B conseguiu ler (o valor já atualizado)
            pausaB.Liberar();
            (await NoMaximoAsync(b, "Reserva B")).ShouldBe(ResultadoReserva.Reservado);

            (await EstoqueAsync(1)).ShouldBe(5);
        }
        finally
        {
            pausaA.Liberar();
            pausaB.Liberar();
            await DrenarAsync(a, b);
        }
    }

    [Fact]
    public async Task ReservarComUpdLock_EnquantoSeguraOLock_LeitorComumNaoEBloqueado()
    {
        var servico = new ReservaDeEstoque(Cs);
        using var pausaA = new PontoDeParada("A");
        var leitor = await AbrirSessaoAsync("Leitor");
        await leitor.ExecutarAsync("SET LOCK_TIMEOUT 0;");

        var a = servico.ReservarComUpdLockAsync(1, 3, pausaA.Gancho);
        try
        {
            await pausaA.EsperarChegadaAsync(a);

            // U é compatível com S: a reserva serializa as RESERVAS, mas não trava a vitrine.
            // (Se você usou XLOCK ou TABLOCKX, este SELECT falha com 1222.)
            var estoque = await leitor.EscalarAsync<int>("SELECT Estoque FROM dbo.Produtos WHERE Id = 1;");
            estoque.ShouldBe(10);
        }
        finally
        {
            pausaA.Liberar();
            await DrenarAsync(a);
        }

        (await a).ShouldBe(ResultadoReserva.Reservado);
        (await EstoqueAsync(1)).ShouldBe(7);
    }

    // ------------------------------------------------------------ Passo 3: rowversion (otimista)

    [Fact]
    public async Task ReservarOtimista_SemConcorrencia_DaBaixaETrocaAVersao()
    {
        var versaoAntes = await EscalarInfraAsync<long>("SELECT CAST(Versao AS bigint) FROM dbo.Produtos WHERE Id = 1;");

        var resultado = await new ReservaDeEstoque(Cs).ReservarOtimistaAsync(1, 4);

        resultado.ShouldBe(ResultadoReserva.Reservado);
        (await EstoqueAsync(1)).ShouldBe(6);
        (await EscalarInfraAsync<long>("SELECT CAST(Versao AS bigint) FROM dbo.Produtos WHERE Id = 1;"))
            .ShouldBeGreaterThan(versaoAntes, "todo UPDATE gera uma rowversion nova");
    }

    [Fact]
    public async Task ReservarOtimista_EstoqueInsuficiente_NaoGrava()
    {
        var resultado = await new ReservaDeEstoque(Cs).ReservarOtimistaAsync(1, 50);

        resultado.ShouldBe(ResultadoReserva.EstoqueInsuficiente);
        (await EstoqueAsync(1)).ShouldBe(10);
    }

    [Fact]
    public async Task ReservarOtimista_IntercaladoSemRetry_SegundaDetectaConflitoENaoSobrescreve()
    {
        var (a, b) = await IntercalarOtimistasAsync(maxTentativasDeB: 1);

        a.ShouldBe(ResultadoReserva.Reservado);
        b.ResultadoB.ShouldBe(ResultadoReserva.Conflito, "B leu a versão antiga; o UPDATE ... AND Versao = @versao tem que afetar 0 linhas");
        b.ChamadasDoGanchoB.ShouldBe(1);
        (await EstoqueAsync(1)).ShouldBe(7, "a baixa de A NÃO pode ser sobrescrita");
    }

    [Fact]
    public async Task ReservarOtimista_IntercaladoComRetry_SegundaReleEConclui()
    {
        var (a, b) = await IntercalarOtimistasAsync(maxTentativasDeB: 3);

        a.ShouldBe(ResultadoReserva.Reservado);
        b.ResultadoB.ShouldBe(ResultadoReserva.Reservado);
        b.ChamadasDoGanchoB.ShouldBe(2, "1ª tentativa deu conflito, 2ª releu (estoque 7, versão nova) e gravou");
        (await EstoqueAsync(1)).ShouldBe(5);
    }

    /// <summary>A lê, B lê (mesma versão), A grava, B tenta gravar.</summary>
    private async Task<(ResultadoReserva A, (ResultadoReserva ResultadoB, int ChamadasDoGanchoB) B)> IntercalarOtimistasAsync(int maxTentativasDeB)
    {
        var servico = new ReservaDeEstoque(Cs);
        using var pausaA = new PontoDeParada("A");
        using var pausaB = new PontoDeParada("B");
        Task<ResultadoReserva>? a = null, b = null;
        try
        {
            a = servico.ReservarOtimistaAsync(1, 3, maxTentativas: 1, pausaA.Gancho);
            await pausaA.EsperarChegadaAsync(a);
            b = servico.ReservarOtimistaAsync(1, 2, maxTentativasDeB, pausaB.Gancho);
            await pausaB.EsperarChegadaAsync(b); // otimista não trava: B lê na hora

            pausaA.Liberar();
            var resultadoA = await NoMaximoAsync(a, "Reserva A");
            pausaB.Liberar();
            var resultadoB = await NoMaximoAsync(b, "Reserva B");
            return (resultadoA, (resultadoB, pausaB.Chamadas));
        }
        finally
        {
            pausaA.Liberar();
            pausaB.Liberar();
            await DrenarAsync(a, b);
        }
    }
}
