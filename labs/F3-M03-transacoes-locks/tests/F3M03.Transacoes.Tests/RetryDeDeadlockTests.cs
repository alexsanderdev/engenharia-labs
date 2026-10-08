using F3M03.Transacoes.Deadlocks;
using F3M03.Transacoes.Tests.Infra;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Time.Testing;

namespace F3M03.Transacoes.Tests;

/// <summary>Passo 5 (parte unitária, sem banco): política de backoff e mecânica do retry.</summary>
public sealed class PoliticaDeRetryTests
{
    [Theory]
    [InlineData(1, 100)]
    [InlineData(2, 200)]
    [InlineData(3, 400)]
    [InlineData(4, 800)]
    [InlineData(5, 1000)] // 1600 passaria do teto
    [InlineData(40, 1000)] // não pode estourar com tentativas altas
    public void CalcularAtraso_BackoffExponencialComTeto(int tentativaQueFalhou, int atrasoEsperadoMs)
    {
        var politica = new PoliticaDeRetry(MaxTentativas: 50, AtrasoBase: TimeSpan.FromMilliseconds(100), AtrasoMaximo: TimeSpan.FromSeconds(1));

        politica.CalcularAtraso(tentativaQueFalhou).ShouldBe(TimeSpan.FromMilliseconds(atrasoEsperadoMs));
    }

    [Fact]
    public async Task Executar_SucessoDePrimeira_ExecutaUmaVezSemEsperar()
    {
        var retry = new RetryDeDeadlock(PoliticaDeRetry.Padrao, new FakeTimeProvider());
        var tentativas = 0;

        var resultado = await retry.ExecutarAsync((t, _) => { tentativas = t; return Task.FromResult(42); });

        resultado.ShouldBe(42);
        tentativas.ShouldBe(1);
    }

    [Fact]
    public async Task Executar_FalhaRetentavel_EsperaOBackoffDoRelogioAntesDeCadaNovaTentativa()
    {
        using var relogio = new RelogioQueAvisa();
        var politica = new PoliticaDeRetry(3, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
        // Costura: "retentável" aqui é TimeoutException, para testar a mecânica sem fabricar SqlException.
        var retry = new RetryDeDeadlock(politica, relogio, ex => ex is TimeoutException);

        var chamadas = 0;
        var execucao = retry.ExecutarAsync<string>((tentativa, _) =>
        {
            Interlocked.Increment(ref chamadas);
            return tentativa == 3 ? Task.FromResult("ok") : throw new TimeoutException($"falha {tentativa}");
        });

        // Falhou a 1ª: tem que estar esperando 100 ms NO RELÓGIO (não no Task.Delay do sistema).
        await relogio.EsperarAlguemComecarAEsperarAsync();
        relogio.Advance(TimeSpan.FromMilliseconds(99));
        Volatile.Read(ref chamadas).ShouldBe(1, "retentou antes do atraso de 100 ms");
        relogio.Advance(TimeSpan.FromMilliseconds(1));

        // Falhou a 2ª: agora o atraso dobra para 200 ms.
        await relogio.EsperarAlguemComecarAEsperarAsync();
        Volatile.Read(ref chamadas).ShouldBe(2);
        relogio.Advance(TimeSpan.FromMilliseconds(199));
        Volatile.Read(ref chamadas).ShouldBe(2, "o atraso da 2ª falha deveria ser 200 ms (backoff exponencial)");
        relogio.Advance(TimeSpan.FromMilliseconds(1));

        (await execucao.WaitAsync(TimeSpan.FromSeconds(5))).ShouldBe("ok");
        chamadas.ShouldBe(3);
    }

    [Fact]
    public async Task Executar_FalhaRetentavelEmTodas_RelancaAExcecaoOriginalDepoisDoMaximo()
    {
        var relogio = new FakeTimeProvider();
        var retry = new RetryDeDeadlock(new PoliticaDeRetry(3, TimeSpan.Zero, TimeSpan.Zero), relogio, ex => ex is TimeoutException);
        var chamadas = 0;

        var ex = await Should.ThrowAsync<TimeoutException>(() =>
            retry.ExecutarAsync<int>((t, _) => { chamadas++; throw new TimeoutException($"falha {t}"); }));

        chamadas.ShouldBe(3);
        ex.Message.ShouldBe("falha 3", "relance a exceção ORIGINAL da última tentativa, sem embrulhar");
    }
}

/// <summary>Passo 5 (parte com banco): retry de vítima de deadlock de verdade.</summary>
public sealed class RetryDeDeadlockTests(BancoFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task EhDeadlock_OutroErroDoSqlServer_NaoERetentadoEPropagaNaHora()
    {
        var retry = new RetryDeDeadlock(PoliticaDeRetry.Padrao, TimeProvider.System);
        var chamadas = 0;

        var ex = await Should.ThrowAsync<SqlException>(() => retry.ExecutarAsync(async (_, ct) =>
        {
            chamadas++;
            await using var conexao = new SqlConnection(Cs);
            await conexao.OpenAsync(ct);
            await using var cmd = new SqlCommand("SELECT 1 / 0;", conexao);
            return await cmd.ExecuteScalarAsync(ct);
        }));

        ex.Number.ShouldBe(8134); // divisão por zero: erro de lógica, repetir não adianta
        chamadas.ShouldBe(1);
        RetryDeDeadlock.EhDeadlock(ex).ShouldBeFalse();
    }

    [Fact]
    public async Task Executar_VitimaDeDeadlock_DesfazRetentaAposBackoffEConclui()
    {
        var (resultadoB, tentativasB, erro) = await CruzarTransferenciasAsync(new PoliticaDeRetry(3, TimeSpan.FromMilliseconds(20), TimeSpan.FromSeconds(1)));

        erro.ShouldBeNull();
        resultadoB.ShouldBeTrue();
        tentativasB.ShouldBe(2, "a 1ª foi vítima do deadlock (1205); a 2ª passou");
        (await EstoqueAsync(1)).ShouldBe(10 - 3 + 4);
        (await EstoqueAsync(2)).ShouldBe(10 + 3 - 4);
    }

    [Fact]
    public async Task Executar_TentativasEsgotadas_RelancaO1205Original()
    {
        var (_, tentativasB, erro) = await CruzarTransferenciasAsync(new PoliticaDeRetry(1, TimeSpan.Zero, TimeSpan.Zero));

        var sqlEx = erro.ShouldBeOfType<SqlException>();
        sqlEx.Number.ShouldBe(1205);
        tentativasB.ShouldBe(1);
        RetryDeDeadlock.EhDeadlock(sqlEx).ShouldBeTrue();
        RetryDeDeadlock.EhDeadlock(new InvalidOperationException("embrulhada (ex.: DbUpdateException do EF)", sqlEx))
            .ShouldBeTrue("procure o 1205 também nas InnerException");
        (await EstoqueAsync(1)).ShouldBe(7, "só a transferência A (1→2, 3 un.) valeu");
        (await EstoqueAsync(2)).ShouldBe(13);
    }

    /// <summary>
    /// A: 1→2 (3 un.), prioridade normal, sem retry. B: 2→1 (4 un.), prioridade BAIXA, dentro do retry.
    /// Orquestração: A trava 1; B trava 2; A pede 2 (espera B); B pede 1 → deadlock, B é a vítima.
    /// </summary>
    private async Task<(bool ResultadoB, int TentativasB, Exception? ErroB)> CruzarTransferenciasAsync(PoliticaDeRetry politica)
    {
        using var pausaA = new PontoDeParada("A");
        using var pausaB = new PontoDeParada("B");
        var retry = new RetryDeDeadlock(politica, TimeProvider.System);
        var tentativasB = 0;
        Task<bool>? a = null, b = null;

        try
        {
            a = new TransferenciaDeEstoque(Cs).MoverNaOrdemDoPedidoAsync(1, 2, 3, pausaA.Gancho);
            await pausaA.EsperarChegadaAsync(a);

            b = retry.ExecutarAsync((tentativa, ct) =>
            {
                tentativasB = tentativa;
                return new TransferenciaDeEstoque(Cs, PrioridadeDeDeadlock.Baixa)
                    .MoverNaOrdemDoPedidoAsync(2, 1, 4, pausaB.Gancho, ct);
            });
            await pausaB.EsperarChegadaAsync(b);

            pausaA.Liberar();               // A pede o produto 2...
            await EsperarSessoesBloqueadasAsync(1); // ...e fica esperando B
            pausaB.Liberar();               // B pede o produto 1: ciclo fechado

            (await NoMaximoAsync(a, "Transferência A")).ShouldBeTrue("A tem prioridade normal e não pode ser a vítima");
            try
            {
                return (await NoMaximoAsync(b, "Transferência B (com retry)"), tentativasB, null);
            }
            catch (SqlException ex)
            {
                return (false, tentativasB, ex);
            }
        }
        finally
        {
            pausaA.Liberar();
            pausaB.Liberar();
            await DrenarAsync(a, b);
        }
    }
}
