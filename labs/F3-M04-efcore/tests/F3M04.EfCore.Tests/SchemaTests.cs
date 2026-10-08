using F3M04.EfCore.Dominio;
using F3M04.EfCore.Persistencia;
using F3M04.EfCore.Tests.Infra;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace F3M04.EfCore.Tests;

/// <summary>
/// Passo 5: o que o modelo virou NO SQL SERVER. Consulta o catálogo do banco
/// (INFORMATION_SCHEMA e sys.*) — é a prova de que a regra está no banco, não só no C#.
/// </summary>
public sealed class SchemaTests(SqlServerFixture fixture) : BancoTestBase(fixture)
{
    private async Task<Dictionary<string, object?>> ColunaAsync(string tabela, string coluna)
    {
        var linhas = await LinhasAsync(
            """
            SELECT DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tabela AND COLUMN_NAME = @coluna
            """,
            ("@tabela", tabela), ("@coluna", coluna));
        linhas.Count.ShouldBe(1, $"coluna {tabela}.{coluna} deveria existir");
        return linhas[0];
    }

    [Fact]
    public async Task Schema_Colunas_TipoTamanhoENulabilidadeNoBanco()
    {
        var sku = await ColunaAsync("Produtos", "Sku");
        sku["DATA_TYPE"].ShouldBe("nvarchar");
        sku["CHARACTER_MAXIMUM_LENGTH"].ShouldBe(30);
        sku["IS_NULLABLE"].ShouldBe("NO");

        var preco = await ColunaAsync("Produtos", "Preco");
        preco["DATA_TYPE"].ShouldBe("decimal");
        preco["NUMERIC_PRECISION"].ShouldBe((byte)18);
        preco["NUMERIC_SCALE"].ShouldBe(2);

        var status = await ColunaAsync("Pedidos", "Status");
        status["DATA_TYPE"].ShouldBe("nvarchar");
        status["CHARACTER_MAXIMUM_LENGTH"].ShouldBe(20);

        var email = await ColunaAsync("Clientes", "Email");
        email["CHARACTER_MAXIMUM_LENGTH"].ShouldBe(200);

        var pedidoId = await ColunaAsync("ItensPedido", "PedidoId");
        pedidoId["IS_NULLABLE"].ShouldBe("NO");
    }

    [Fact]
    public async Task Schema_IndicesUnicosEFks_ExistemNoBanco()
    {
        var unicos = await LinhasAsync(
            """
            SELECT t.name AS Tabela, c.name AS Coluna
            FROM sys.indexes i
            JOIN sys.tables t ON t.object_id = i.object_id
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.is_unique = 1 AND i.is_primary_key = 0
            """);
        unicos.ShouldContain(l => (string)l["Tabela"]! == "Produtos" && (string)l["Coluna"]! == "Sku");
        unicos.ShouldContain(l => (string)l["Tabela"]! == "Clientes" && (string)l["Coluna"]! == "Email");

        var fks = await LinhasAsync(
            """
            SELECT OBJECT_NAME(fk.parent_object_id) AS Filha, OBJECT_NAME(fk.referenced_object_id) AS Pai,
                   fk.delete_referential_action_desc AS AoApagar
            FROM sys.foreign_keys fk
            """);
        fks.ShouldContain(l => (string)l["Filha"]! == "ItensPedido" && (string)l["Pai"]! == "Pedidos" && (string)l["AoApagar"]! == "CASCADE");
        fks.ShouldContain(l => (string)l["Filha"]! == "ItensPedido" && (string)l["Pai"]! == "Produtos" && (string)l["AoApagar"]! == "NO_ACTION");
        fks.ShouldContain(l => (string)l["Filha"]! == "Pedidos" && (string)l["Pai"]! == "Clientes" && (string)l["AoApagar"]! == "NO_ACTION");
    }

    [Fact]
    public async Task Schema_SkuDuplicado_BancoRecusaComErro2601()
    {
        await using (var db = NovoContexto())
        {
            db.Produtos.Add(new Produto(Guid.NewGuid(), "DUPLICADO", "Primeiro", 10m));
            await db.SaveChangesAsync(Ct);
        }

        await using var outro = NovoContexto();
        outro.Produtos.Add(new Produto(Guid.NewGuid(), "DUPLICADO", "Segundo", 20m));

        var erro = await Should.ThrowAsync<DbUpdateException>(() => outro.SaveChangesAsync(Ct));
        erro.InnerException.ShouldBeOfType<SqlException>().Number.ShouldBe(2601);
    }

    [Fact]
    public async Task Seed_CatalogoInicial_ExisteNoBancoCriado()
    {
        var skus = await LinhasAsync("SELECT Sku, Preco FROM Produtos WHERE Sku LIKE 'SEED-%' ORDER BY Sku");

        skus.Select(l => (string)l["Sku"]!).ShouldBe(["SEED-CADERNO", "SEED-CANETA", "SEED-MOCHILA"]);
        skus.Single(l => (string)l["Sku"]! == "SEED-MOCHILA")["Preco"].ShouldBe(189.00m);
    }
}
