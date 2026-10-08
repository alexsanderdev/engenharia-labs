using F3M03.Transacoes.Estoque;
using F3M03.Transacoes.Tests.Infra;
using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Tests;

/// <summary>Passo 4: relatório consistente sem bloquear quem grava (SNAPSHOT).</summary>
public sealed class RelatorioDeEstoqueTests(BancoFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task Gerar_SemConcorrencia_TotalBateComOsItens()
    {
        var relatorio = await new GeradorDeRelatorio(Cs).GerarAsync();

        relatorio.SomaDoEstoque.ShouldBe(30);
        relatorio.Itens.Select(i => i.Sku).ShouldBe(["TECLADO", "MOUSE", "MONITOR"]);
        relatorio.Consistente.ShouldBeTrue();
    }

    [Fact]
    public async Task Gerar_EntradaDeEstoqueEntreAsConsultas_RelatorioContinuaConsistenteEEscritorNaoEspera()
    {
        var escritor = await AbrirSessaoAsync("Escritor");
        await escritor.ExecutarAsync("SET LOCK_TIMEOUT 500;");
        SqlException? erroDoEscritor = null;

        // Entre a consulta do total e a do detalhe, chega uma nota fiscal: +7 teclados (commit).
        var relatorio = await NoMaximoAsync(new GeradorDeRelatorio(Cs).GerarAsync(async () =>
        {
            try
            {
                await escritor.ExecutarAsync("UPDATE dbo.Produtos SET Estoque = Estoque + 7 WHERE Id = 1;");
            }
            catch (SqlException ex) when (ex.Number == 1222)
            {
                erroDoEscritor = ex;
            }
        }), "Relatório");

        erroDoEscritor.ShouldBeNull(
            "o escritor ficou bloqueado pelo relatório (REPEATABLE READ/SERIALIZABLE seguram S locks): use SNAPSHOT");
        relatorio.Consistente.ShouldBeTrue(
            $"total {relatorio.SomaDoEstoque} ≠ soma dos itens {relatorio.Itens.Sum(i => i.Estoque)}: " +
            "as duas consultas viram estados diferentes do banco");
        relatorio.SomaDoEstoque.ShouldBe(30, "a foto é a do início do relatório");
        (await EstoqueAsync(1)).ShouldBe(17, "e a entrada de estoque foi gravada normalmente");
    }
}
