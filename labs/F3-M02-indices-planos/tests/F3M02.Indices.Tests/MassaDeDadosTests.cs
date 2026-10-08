using F3M02.Indices.Tests.Infra;

namespace F3M02.Indices.Tests;

/// <summary>Sanidade da infra: a massa foi carregada e é sempre a mesma (passa desde o início).</summary>
public sealed class MassaDeDadosTests(SqlServerFixture fixture) : PlanoTestBase(fixture)
{
    [Fact]
    public async Task Massa_CargaDeterministica_TemDuzentosMilPedidosEItensProporcionais()
    {
        var linhas = await ConsultarAsync("""
            SELECT (SELECT COUNT(*) FROM dbo.Clientes),
                   (SELECT COUNT(*) FROM dbo.Produtos),
                   (SELECT COUNT(*) FROM dbo.Pedidos),
                   (SELECT COUNT(*) FROM dbo.ItensPedido),
                   (SELECT COUNT(*) FROM dbo.Pedidos WHERE Status = 'Created')
            """);

        var l = linhas.Single();
        ((int)l[0]!).ShouldBe(20_000);
        ((int)l[1]!).ShouldBe(1_000);
        ((int)l[2]!).ShouldBe(200_000);
        ((int)l[3]!).ShouldBe(500_000);
        ((int)l[4]!).ShouldBe(1_999); // ~1% em aberto
        TestContext.Current.TestOutputHelper?.WriteLine($"Carga da massa: {Fixture.TempoDeCarga.TotalSeconds:N1} s");
    }
}
