using F3M01.Sql.Tests.Infra;

namespace F3M01.Sql.Tests;

/// <summary>
/// Parte A, passo 1: a ESTRUTURA do seu Schema.sql, lida dos metadados do SQL Server
/// (INFORMATION_SCHEMA e catálogo sys.*). Nenhum dado é inserido aqui.
/// </summary>
public sealed class ParteA_SchemaTests(SqlServerFixture fixture) : ParteATestBase(fixture)
{
    [Fact]
    public async Task Tabelas_QuatroTabelasDoOrderFlow_ExistemNoSchemaDbo()
    {
        var linhas = await ConsultarAsync(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_TYPE = 'BASE TABLE'");

        linhas.Select(l => l.Texto("TABLE_NAME"))
            .ShouldBe(["Clientes", "ItensPedido", "Pedidos", "Produtos"], ignoreOrder: true);
    }

    [Theory]
    [InlineData("Clientes", "Id", "int", false)]
    [InlineData("Clientes", "Nome", "nvarchar(150)", false)]
    [InlineData("Clientes", "Email", "nvarchar(254)", false)]
    [InlineData("Clientes", "IndicadoPorId", "int", true)]
    [InlineData("Clientes", "CriadoEm", "datetime2", false)]
    [InlineData("Produtos", "Id", "int", false)]
    [InlineData("Produtos", "Sku", "varchar(20)", false)]
    [InlineData("Produtos", "Nome", "nvarchar(200)", false)]
    [InlineData("Produtos", "Preco", "decimal(18,2)", false)]
    [InlineData("Produtos", "Ativo", "bit", false)]
    [InlineData("Pedidos", "Id", "int", false)]
    [InlineData("Pedidos", "ClienteId", "int", false)]
    [InlineData("Pedidos", "CriadoEm", "datetime2", false)]
    [InlineData("Pedidos", "Status", "varchar(20)", false)]
    [InlineData("Pedidos", "Total", "decimal(18,2)", false)]
    [InlineData("ItensPedido", "PedidoId", "int", false)]
    [InlineData("ItensPedido", "ProdutoId", "int", false)]
    [InlineData("ItensPedido", "Quantidade", "int", false)]
    [InlineData("ItensPedido", "PrecoUnitario", "decimal(18,2)", false)]
    public async Task Coluna_TipoTamanhoENulabilidade_ConformeContrato(string tabela, string coluna, string tipo, bool aceitaNulo)
    {
        var linhas = await ConsultarAsync($"""
            SELECT DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE, IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = '{tabela}' AND COLUMN_NAME = '{coluna}'
            """);

        linhas.Count.ShouldBe(1, $"A coluna {tabela}.{coluna} não existe.");
        var c = linhas[0];
        var tipoReal = c.Texto("DATA_TYPE") switch
        {
            "varchar" or "nvarchar" or "char" or "nchar" when c["CHARACTER_MAXIMUM_LENGTH"] is int tamanho =>
                $"{c.Texto("DATA_TYPE")}({(tamanho == -1 ? "max" : tamanho.ToString(System.Globalization.CultureInfo.InvariantCulture))})",
            "decimal" or "numeric" => $"{c.Texto("DATA_TYPE")}({c["NUMERIC_PRECISION"]},{c["NUMERIC_SCALE"]})",
            var outro => outro,
        };

        tipoReal.ShouldBe(tipo, $"Tipo de {tabela}.{coluna}");
        (c.Texto("IS_NULLABLE") == "YES").ShouldBe(aceitaNulo,
            aceitaNulo ? $"{tabela}.{coluna} deve aceitar NULL." : $"{tabela}.{coluna} deve ser NOT NULL.");
    }

    [Theory]
    [InlineData("Clientes", "Id")]
    [InlineData("Produtos", "Id")]
    [InlineData("Pedidos", "Id")]
    [InlineData("ItensPedido", "PedidoId,ProdutoId")]
    public async Task ChavePrimaria_CadaTabela_TemPkNasColunasCertas(string tabela, string colunas)
    {
        var linhas = await ConsultarAsync($"""
            SELECT STRING_AGG(k.COLUMN_NAME, ',') WITHIN GROUP (ORDER BY k.ORDINAL_POSITION) AS Colunas
            FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS t
            JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS k
              ON k.CONSTRAINT_NAME = t.CONSTRAINT_NAME AND k.TABLE_SCHEMA = t.TABLE_SCHEMA
            WHERE t.TABLE_SCHEMA = 'dbo' AND t.TABLE_NAME = '{tabela}' AND t.CONSTRAINT_TYPE = 'PRIMARY KEY'
            """);

        (linhas.Single()["Colunas"] as string).ShouldBe(colunas, $"PK de {tabela}");
    }

    [Theory]
    [InlineData("Clientes")]
    [InlineData("Produtos")]
    [InlineData("Pedidos")]
    public async Task ChaveSurrogate_Id_EhGeradoPeloBanco(string tabela)
    {
        var linhas = await ConsultarAsync(
            $"SELECT COLUMNPROPERTY(OBJECT_ID('dbo.{tabela}'), 'Id', 'IsIdentity') AS EhIdentity");

        linhas.Single()["EhIdentity"].ShouldBe(1, $"{tabela}.Id deve ser IDENTITY.");
    }

    [Theory]
    [InlineData("Pedidos", "ClienteId", "Clientes")]
    [InlineData("ItensPedido", "PedidoId", "Pedidos")]
    [InlineData("ItensPedido", "ProdutoId", "Produtos")]
    [InlineData("Clientes", "IndicadoPorId", "Clientes")]
    public async Task ChaveEstrangeira_Relacionamento_ApontaParaOIdDaTabelaPai(string tabela, string coluna, string tabelaPai)
    {
        var linhas = await ConsultarAsync($"""
            SELECT OBJECT_NAME(fkc.referenced_object_id) AS TabelaPai,
                   COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ColunaPai
            FROM sys.foreign_key_columns AS fkc
            WHERE fkc.parent_object_id = OBJECT_ID('dbo.{tabela}')
              AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = '{coluna}'
            """);

        linhas.Count.ShouldBe(1, $"Falta a FK {tabela}.{coluna} → {tabelaPai}.Id.");
        linhas[0].Texto("TabelaPai").ShouldBe(tabelaPai);
        linhas[0].Texto("ColunaPai").ShouldBe("Id");
    }

    [Theory]
    [InlineData("Clientes", "Email")]
    [InlineData("Produtos", "Sku")]
    public async Task ChaveNatural_Coluna_TemRestricaoDeUnicidade(string tabela, string coluna)
    {
        // UNIQUE constraint ou índice único (sem filtro) cuja ÚNICA coluna-chave é a coluna.
        var linhas = await ConsultarAsync($"""
            SELECT COUNT(*) AS Quantos
            FROM sys.indexes AS i
            WHERE i.object_id = OBJECT_ID('dbo.{tabela}')
              AND i.is_unique = 1 AND i.is_primary_key = 0 AND i.has_filter = 0
              AND (SELECT COUNT(*) FROM sys.index_columns AS ic
                   WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0) = 1
              AND EXISTS (SELECT 1 FROM sys.index_columns AS ic
                          WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                            AND ic.is_included_column = 0 AND COL_NAME(ic.object_id, ic.column_id) = '{coluna}')
            """);

        linhas.Single().Inteiro("Quantos").ShouldBeGreaterThan(0, $"{tabela}.{coluna} deve ser única (UNIQUE).");
    }
}
