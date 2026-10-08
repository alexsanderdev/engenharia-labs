using F4M06.Integracao.Tests.Infra;
using Microsoft.Data.SqlClient;

namespace F4M06.Integracao.Tests;

/// <summary>
/// Dados privados por módulo também NO BANCO: cada módulo tem o próprio schema e nenhuma FK
/// atravessa a fronteira (é o que permite, um dia, mover um módulo para outro banco).
/// </summary>
[Collection(ColecaoIntegracao.Nome)]
public sealed class SchemasPorModuloTests(OrderFlowFixture fixture) : IntegracaoTestBase(fixture)
{
    [Fact]
    public async Task CadaModulo_TemSuasTabelasNoProprioSchema()
    {
        var tabelas = await ListarAsync("""
            SELECT s.name + '.' + t.name
            FROM sys.tables t JOIN sys.schemas s ON s.schema_id = t.schema_id
            ORDER BY 1
            """);

        tabelas.ShouldBe(
            [
                "catalogo.Produtos",
                "clientes.Clientes",
                "clientes.PedidosContabilizados",
                "pedidos.ItensPedido",
                "pedidos.Pedidos",
            ],
            "cada módulo cria as tabelas no schema dele (catalogo, pedidos, clientes) e nada fica no dbo");
    }

    [Fact]
    public async Task NenhumaForeignKey_AtravessaAFronteiraDeModulo()
    {
        var fksEntreSchemas = await ListarAsync("""
            SELECT fk.name + ': ' + OBJECT_SCHEMA_NAME(fk.parent_object_id) + ' -> ' + OBJECT_SCHEMA_NAME(fk.referenced_object_id)
            FROM sys.foreign_keys fk
            WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) <> OBJECT_SCHEMA_NAME(fk.referenced_object_id)
            """);

        fksEntreSchemas.ShouldBeEmpty("referência a outro módulo é por Id (valor), nunca por FK");
    }

    private async Task<List<string>> ListarAsync(string sql)
    {
        await using var conexao = new SqlConnection(Fixture.ConnectionString);
        await conexao.OpenAsync(Ct);
        await using var comando = new SqlCommand(sql, conexao);
        await using var leitor = await comando.ExecuteReaderAsync(Ct);

        var linhas = new List<string>();
        while (await leitor.ReadAsync(Ct))
            linhas.Add(leitor.GetString(0));
        return linhas;
    }
}
