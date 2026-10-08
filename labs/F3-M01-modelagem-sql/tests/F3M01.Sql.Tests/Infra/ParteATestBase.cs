using Microsoft.Data.SqlClient;

namespace F3M01.Sql.Tests.Infra;

/// <summary>
/// Base dos testes da Parte A. Isolamento: todo teste que escreve roda numa transação
/// que é SEMPRE revertida, então o banco da modelagem continua vazio entre os testes.
/// </summary>
public abstract class ParteATestBase(SqlServerFixture fixture)
{
    /// <summary>Números de erro do SQL Server usados nos asserts.</summary>
    protected const int ViolacaoDeCheckOuFk = 547;
    protected const int NuloEmColunaNotNull = 515;
    protected const int ViolacaoDeUnique = 2627;
    protected const int ViolacaoDeIndiceUnico = 2601;

    /// <summary>Consulta somente leitura (metadados) no banco da modelagem.</summary>
    protected async Task<List<Linha>> ConsultarAsync(string sql)
    {
        await using var conexao = new SqlConnection(await fixture.BancoDaModelagemAsync());
        await conexao.OpenAsync(TestContext.Current.CancellationToken);
        await using var cmd = new SqlCommand(sql, conexao);
        return await Linha.LerTodasAsync(cmd);
    }

    /// <summary>
    /// Executa <paramref name="sql"/> numa transação revertida no fim e devolve as linhas do último SELECT
    /// (útil para <c>INSERT ... ; SELECT ...</c>).
    /// </summary>
    protected async Task<List<Linha>> EmTransacaoRevertidaAsync(string sql)
    {
        await using var conexao = new SqlConnection(await fixture.BancoDaModelagemAsync());
        await conexao.OpenAsync(TestContext.Current.CancellationToken);
        await using var tx = (SqlTransaction)await conexao.BeginTransactionAsync(TestContext.Current.CancellationToken);
        try
        {
            await using var cmd = new SqlCommand(sql, conexao, tx);
            return await Linha.LerTodasAsync(cmd);
        }
        finally
        {
            if (tx.Connection is not null) await tx.RollbackAsync();
        }
    }

    /// <summary>
    /// Executa <paramref name="sql"/> numa transação revertida e exige que o SQL Server o RECUSE.
    /// Devolve a <see cref="SqlException"/> para o teste conferir o número do erro.
    /// </summary>
    protected async Task<SqlException> DeveSerRecusadoAsync(string sql)
    {
        try
        {
            await EmTransacaoRevertidaAsync(sql);
        }
        catch (SqlException ex)
        {
            return ex;
        }

        throw new Xunit.Sdk.XunitException(
            $"O banco ACEITOU um dado que deveria recusar. Falta uma constraint no Schema.sql.\nSQL executado:\n{sql}");
    }

    /// <summary>INSERT de um cliente válido; devolve o SQL que deixa o Id em @clienteId.</summary>
    protected static string InserirCliente(string email = "ana@orderflow.dev") =>
        $"""
        DECLARE @clienteId int;
        INSERT INTO dbo.Clientes (Nome, Email) VALUES (N'Ana Souza', N'{email}');
        SET @clienteId = SCOPE_IDENTITY();
        """;

    /// <summary>INSERT de um produto válido; devolve o SQL que deixa o Id em @produtoId.</summary>
    protected static string InserirProduto(string sku = "TEC-001") =>
        $"""
        DECLARE @produtoId int;
        INSERT INTO dbo.Produtos (Sku, Nome, Preco) VALUES ('{sku}', N'Teclado mecânico', 350.00);
        SET @produtoId = SCOPE_IDENTITY();
        """;

    /// <summary>INSERT de um pedido válido (precisa de @clienteId); deixa o Id em @pedidoId.</summary>
    protected static string InserirPedido() =>
        """
        DECLARE @pedidoId int;
        INSERT INTO dbo.Pedidos (ClienteId, Status, Total) VALUES (@clienteId, 'Created', 700.00);
        SET @pedidoId = SCOPE_IDENTITY();
        """;
}
