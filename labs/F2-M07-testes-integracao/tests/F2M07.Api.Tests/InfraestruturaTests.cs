using System.Net;
using System.Net.Http.Json;
using F2M07.Api.Pedidos;
using F2M07.Api.Tests.Infra;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace F2M07.Api.Tests;

/// <summary>
/// Passos 1 a 4: a infraestrutura de teste funciona? Container no ar, schema criado,
/// Respawn limpando entre testes e autenticação fake no lugar da real.
/// </summary>
[Collection(ColecaoIntegracao.Nome)]
public sealed class InfraestruturaTests(ApiFixture fixture) : IntegracaoTestBase(fixture)
{
    // ---------- Passo 1: container ----------

    [Fact]
    public async Task Fixture_ContainerNoAr_SqlServer2022AceitaConexao()
    {
        var builder = new SqlConnectionStringBuilder(Fixture.ConnectionString);
        builder.InitialCatalog.ShouldBe(ApiFixture.NomeDoBanco);

        // O banco de teste só existe depois do Passo 2; aqui conectamos no "master" do container.
        builder.InitialCatalog = "master";
        await using var conexao = new SqlConnection(builder.ConnectionString);
        await conexao.OpenAsync(Ct);
        await using var comando = new SqlCommand("SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(32))", conexao);

        var versao = (string?)await comando.ExecuteScalarAsync(Ct);

        versao.ShouldNotBeNull().ShouldStartWith("16."); // 16.x = SQL Server 2022
    }

    // ---------- Passo 2: factory + schema ----------

    [Fact]
    public async Task Fixture_SchemaCriado_TabelasDoModeloExistem()
    {
        var tabelas = await ComBancoAsync(db => db.Database
            .SqlQueryRaw<string>("SELECT TABLE_NAME AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'")
            .ToListAsync(Ct));

        tabelas.ShouldBe(["ItensPedido", "Pedidos", "Produtos"], ignoreOrder: true);
    }

    // ---------- Passo 3: Respawn ----------

    [Fact]
    public async Task Reset_DepoisDeInserirDados_ApagaTudoMasMantemOSchema()
    {
        var produto = await SemearProdutoAsync("RESET-1", 10m);
        await SemearPedidoAsync(ClienteA, produto, StatusPedido.Created);

        await Fixture.ResetarBancoAsync();

        (await ComBancoAsync(db => db.Produtos.CountAsync(Ct))).ShouldBe(0);
        (await ComBancoAsync(db => db.Pedidos.CountAsync(Ct))).ShouldBe(0);
        // Se o Reset tivesse dropado tabelas, este insert explodiria.
        await SemearProdutoAsync("RESET-2", 10m);
    }

    // Os dois testes abaixo usam o MESMO SKU (índice único). Em qualquer ordem, o segundo só passa
    // se o banco foi limpo antes dele: é a prova de isolamento entre testes.

    [Fact]
    public async Task Isolamento_PrimeiroTeste_ComecaComBancoVazio()
    {
        (await ComBancoAsync(db => db.Produtos.CountAsync(Ct))).ShouldBe(0);
        await SemearProdutoAsync("ISOLADO", 10m);
    }

    [Fact]
    public async Task Isolamento_SegundoTeste_ComecaComBancoVazio()
    {
        (await ComBancoAsync(db => db.Produtos.CountAsync(Ct))).ShouldBe(0);
        await SemearProdutoAsync("ISOLADO", 10m);
    }

    // ---------- Passo 4: autenticação fake ----------

    [Fact]
    public async Task Auth_SemUsuario_Retorna401()
    {
        using var client = CriarCliente();

        var response = await client.GetAsync("/pedidos", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Auth_ComUsuarioDeTeste_Retorna200ComListaVazia()
    {
        using var client = CriarCliente(ClienteA);

        var response = await client.GetAsync("/pedidos", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<PedidoResponse[]>(Ct)).ShouldBeEmpty();
    }
}
