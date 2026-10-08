using F3M06.Dapper.Leitura;
using F3M06.Dapper.Tests.Infra;

namespace F3M06.Dapper.Tests;

/// <summary>Passos 1 a 3: consultas parametrizadas, SQL injection, LIKE e listas no IN.</summary>
public sealed class ProdutoQueriesTests(BancoFixture banco)
{
    private readonly ProdutoQueries _queries = new(banco.ConnectionString);
    private readonly ConsultasInseguras _inseguras = new(banco.ConnectionString);
    private DadosDeTeste Dados => banco.Dados;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---------- Passo 1: parâmetro ----------

    [Fact]
    public async Task ObterPorSku_SkuExistente_MapeiaTodasAsColunas()
    {
        var produto = await _queries.ObterPorSkuAsync("MON-001", Ct);

        produto.ShouldNotBeNull();
        produto.ShouldBe(new ProdutoResumo
        {
            Id = Dados.Monitor.Id,
            Sku = "MON-001",
            Nome = "Monitor 27 polegadas",
            Preco = 1500.00m,
            Ativo = true,
        });
    }

    [Fact]
    public async Task ObterPorSku_SkuInexistente_RetornaNull()
    {
        var produto = await _queries.ObterPorSkuAsync("NAO-EXISTE", Ct);

        produto.ShouldBeNull();
    }

    // ---------- Passo 2: busca segura ----------

    [Fact]
    public async Task BuscarPorNome_TermoNormal_RetornaSoAtivosOrdenadosPorNome()
    {
        var produtos = await _queries.BuscarPorNomeAsync("o", Ct);

        // Todos os ativos têm "o" no nome; a Webcam (inativa) também tem, mas não pode aparecer.
        produtos.Select(p => p.Sku).ShouldBe(["CAB-001", "MON-001", "MOU-001", "SUP-001", "TEC-001"]);
        produtos.ShouldAllBe(p => p.Ativo);
    }

    [Fact]
    public async Task BuscarPorNome_TermoComApostrofo_EncontraEmVezDeQuebrarOSql()
    {
        // Na versão concatenada, o apóstrofo fecha a string SQL e o comando dá erro de sintaxe.
        var produtos = await _queries.BuscarPorNomeAsync("D'Angelo", Ct);

        produtos.Select(p => p.Sku).ShouldBe(["SUP-001"]);
    }

    [Fact]
    public async Task SqlInjection_OrUmIgualAUm_ConcatenadaVazaInativos_ParametrizadaNao()
    {
        const string ataque = "x%' OR 1=1 --";

        var vulneravel = await _inseguras.BuscarProdutosPorNomeConcatenandoAsync(ataque);
        var segura = await _queries.BuscarPorNomeAsync(ataque, Ct);

        // A versão concatenada executou "... OR 1=1" e devolveu até o produto inativo.
        vulneravel.Count.ShouldBe(Dados.Produtos.Count);
        vulneravel.ShouldContain(p => !p.Ativo);
        // A parametrizada procurou o TEXTO "x%' OR 1=1 --" no nome: nenhum produto tem isso.
        segura.ShouldBeEmpty();
    }

    [Fact]
    public async Task SqlInjection_UnionComClientes_ConcatenadaVazaEmails_ParametrizadaNao()
    {
        const string ataque = "' UNION SELECT Id, Email, Nome, 0, CAST(1 AS bit) FROM Clientes --";

        var vulneravel = await _inseguras.BuscarProdutosPorNomeConcatenandoAsync(ataque);
        var segura = await _queries.BuscarPorNomeAsync(ataque, Ct);

        // Os e-mails dos clientes saíram na coluna "Sku" de uma busca de produtos.
        vulneravel.Select(p => p.Sku).ShouldContain("ana@orderflow.dev");
        segura.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("[a-z]")]
    public async Task BuscarPorNome_CuringasDoLike_SaoTratadosComoTextoLiteral(string termo)
    {
        // Parâmetro evita injection, mas dentro do LIKE "%" e "_" ainda são curingas.
        // Nenhum nome de produto contém esses caracteres, então o resultado tem que ser vazio.
        var produtos = await _queries.BuscarPorNomeAsync(termo, Ct);

        produtos.ShouldBeEmpty();
    }

    // ---------- Passo 3: listas no IN ----------

    [Fact]
    public async Task ObterPorIds_ListaVazia_RetornaVazio()
    {
        var produtos = await _queries.ObterPorIdsAsync([], Ct);

        produtos.ShouldBeEmpty();
    }

    [Fact]
    public async Task ObterPorIds_IdsRepetidosEInexistentes_RetornaSoExistentesSemDuplicarOrdenadosPorSku()
    {
        Guid[] ids = [Dados.Teclado.Id, Guid.NewGuid(), Dados.Cabo.Id, Dados.Teclado.Id, Dados.WebcamInativa.Id];

        var produtos = await _queries.ObterPorIdsAsync(ids, Ct);

        produtos.Select(p => p.Sku).ShouldBe(["CAB-001", "TEC-001", "WEB-001"]);
    }

    [Fact]
    public async Task ObterPorIds_MaisDe2100Ids_NaoEstouraOLimiteDeParametrosDoSqlServer()
    {
        // "WHERE Id IN @Ids" do Dapper vira um parâmetro por item: acima de 2.100 o SQL Server recusa o comando.
        var ids = Enumerable.Range(0, 2_500).Select(_ => Guid.NewGuid()).ToList();
        ids.Add(Dados.Mouse.Id);
        ids.Add(Dados.Monitor.Id);

        var produtos = await _queries.ObterPorIdsAsync(ids, Ct);

        produtos.Select(p => p.Sku).ShouldBe(["MON-001", "MOU-001"]);
    }
}
