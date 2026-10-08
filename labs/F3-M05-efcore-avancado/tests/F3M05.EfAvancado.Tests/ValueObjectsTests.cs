using F3M05.EfAvancado.Dominio;
using F3M05.EfAvancado.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F3M05.EfAvancado.Tests;

/// <summary>Passo 2: value conversion (Sku) e owned type (Dinheiro).</summary>
public sealed class ValueObjectsTests(SqlServerFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task Produto_SkuEPreco_GravamEmColunasProprias()
    {
        await using (var db = NovoContexto())
        {
            db.Produtos.Add(new Produto { Sku = new Sku("cad-001"), Nome = "Cadeira", Preco = new Dinheiro(1299.90m, "brl") });
            await db.SaveChangesAsync(Ct);
        }

        var linha = (await LinhasAsync("SELECT Sku, PrecoValor, PrecoMoeda FROM Produtos WHERE Nome = 'Cadeira'")).Single();
        linha["Sku"].ShouldBe("CAD-001");
        linha["PrecoValor"].ShouldBe(1299.90m);
        linha["PrecoMoeda"].ShouldBe("BRL");

        var colunas = await LinhasAsync(
            """
            SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
            FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Produtos'
            """);
        var sku = colunas.Single(c => (string)c["COLUMN_NAME"]! == "Sku");
        sku["DATA_TYPE"].ShouldBe("varchar");
        sku["CHARACTER_MAXIMUM_LENGTH"].ShouldBe(20);
        var valor = colunas.Single(c => (string)c["COLUMN_NAME"]! == "PrecoValor");
        valor["NUMERIC_PRECISION"].ShouldBe((byte)18);
        valor["NUMERIC_SCALE"].ShouldBe(2);
        colunas.Single(c => (string)c["COLUMN_NAME"]! == "PrecoMoeda")["DATA_TYPE"].ShouldBe("char");
    }

    [Fact]
    public async Task FiltroPorSku_TraduzidoParaSql_EValueObjectsVoltamDoBanco()
    {
        var produto = await SemearProdutoAsync("Monitor", 899.99m);

        await using var db = NovoContexto();
        var consulta = db.Produtos.AsNoTracking().Where(p => p.Sku == produto.Sku);

        var sql = consulta.ToQueryString();
        sql.ShouldContain("WHERE [p].[Sku] = @", Case.Insensitive, "o filtro tem de rodar NO BANCO, comparando a coluna convertida");

        var lido = await consulta.SingleAsync(Ct);
        lido.Sku.ShouldBe(produto.Sku);
        lido.Preco.ShouldBe(Dinheiro.Reais(899.99m));
    }
}
