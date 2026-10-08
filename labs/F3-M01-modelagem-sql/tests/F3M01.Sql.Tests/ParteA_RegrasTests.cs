using F3M01.Sql.Tests.Infra;

namespace F3M01.Sql.Tests;

/// <summary>
/// Parte A, passo 2: o banco DEFENDE as regras de negócio. Cada teste tenta gravar um dado
/// (numa transação revertida) e verifica se o SQL Server aceitou ou recusou, e com qual erro.
/// </summary>
public sealed class ParteA_RegrasTests(SqlServerFixture fixture) : ParteATestBase(fixture)
{
    [Fact]
    public async Task Inserts_DadosValidosDoOrderFlow_SaoAceitos()
    {
        var linhas = await EmTransacaoRevertidaAsync($"""
            {InserirCliente()}
            {InserirProduto()}
            {InserirPedido()}
            INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedidoId, @produtoId, 2, 350.00);
            INSERT INTO dbo.Produtos (Sku, Nome, Preco) VALUES ('BRINDE-01', N'Adesivo (brinde)', 0.00); -- preço zero é permitido
            SELECT COUNT(*) AS Itens FROM dbo.ItensPedido WHERE PedidoId = @pedidoId;
            """);

        linhas.Single().Inteiro("Itens").ShouldBe(1);
    }

    [Fact]
    public async Task Inserts_SemColunasOpcionais_AplicamOsDefaults()
    {
        var linhas = await EmTransacaoRevertidaAsync($"""
            {InserirCliente()}
            INSERT INTO dbo.Produtos (Sku, Nome, Preco) VALUES ('MOU-001', N'Mouse', 120.00);
            INSERT INTO dbo.Pedidos (ClienteId) VALUES (@clienteId);
            SELECT p.Status, p.Total,
                   DATEDIFF(SECOND, p.CriadoEm, SYSUTCDATETIME()) AS IdadePedidoSeg,
                   DATEDIFF(SECOND, c.CriadoEm, SYSUTCDATETIME()) AS IdadeClienteSeg,
                   (SELECT CAST(Ativo AS int) FROM dbo.Produtos WHERE Sku = 'MOU-001') AS ProdutoAtivo
            FROM dbo.Pedidos AS p
            JOIN dbo.Clientes AS c ON c.Id = p.ClienteId;
            """);

        var l = linhas.Single();
        l.Texto("Status").ShouldBe("Created");
        l.Valor("Total").ShouldBe(0m);
        l.Inteiro("ProdutoAtivo").ShouldBe(1);
        // DEFAULT de data em UTC (SYSUTCDATETIME), gravado agora.
        Math.Abs(l.Inteiro("IdadePedidoSeg")).ShouldBeLessThan(60);
        Math.Abs(l.Inteiro("IdadeClienteSeg")).ShouldBeLessThan(60);
    }

    [Fact]
    public async Task Cliente_SemNome_EhRecusadoPorNotNull()
    {
        var erro = await DeveSerRecusadoAsync("INSERT INTO dbo.Clientes (Nome, Email) VALUES (NULL, N'x@orderflow.dev');");

        erro.Number.ShouldBe(NuloEmColunaNotNull);
    }

    [Fact]
    public async Task Cliente_EmailDuplicado_EhRecusadoPeloUnique()
    {
        var erro = await DeveSerRecusadoAsync($"""
            {InserirCliente("ana@orderflow.dev")}
            INSERT INTO dbo.Clientes (Nome, Email) VALUES (N'Outra Ana', N'ana@orderflow.dev');
            """);

        erro.Number.ShouldBeOneOf(ViolacaoDeUnique, ViolacaoDeIndiceUnico);
    }

    [Fact]
    public async Task Produto_SkuDuplicado_EhRecusadoPeloUnique()
    {
        var erro = await DeveSerRecusadoAsync($"""
            {InserirProduto("TEC-001")}
            INSERT INTO dbo.Produtos (Sku, Nome, Preco) VALUES ('TEC-001', N'Outro teclado', 99.90);
            """);

        erro.Number.ShouldBeOneOf(ViolacaoDeUnique, ViolacaoDeIndiceUnico);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("-350.00")]
    public async Task Produto_PrecoNegativo_EhRecusadoPeloCheck(string preco)
    {
        var erro = await DeveSerRecusadoAsync(
            $"INSERT INTO dbo.Produtos (Sku, Nome, Preco) VALUES ('NEG-001', N'Produto', {preco});");

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
        erro.Message.ShouldContain("CHECK");
    }

    [Theory]
    [InlineData("Shipped")]
    [InlineData("Pago")]
    [InlineData("")]
    public async Task Pedido_StatusForaDaLista_EhRecusadoPeloCheck(string status)
    {
        var erro = await DeveSerRecusadoAsync($"""
            {InserirCliente()}
            INSERT INTO dbo.Pedidos (ClienteId, Status, Total) VALUES (@clienteId, '{status}', 10.00);
            """);

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
        erro.Message.ShouldContain("CHECK");
    }

    [Fact]
    public async Task Pedido_TotalNegativo_EhRecusadoPeloCheck()
    {
        var erro = await DeveSerRecusadoAsync($"""
            {InserirCliente()}
            INSERT INTO dbo.Pedidos (ClienteId, Status, Total) VALUES (@clienteId, 'Created', -1.00);
            """);

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
        erro.Message.ShouldContain("CHECK");
    }

    [Theory]
    [InlineData(0, "350.00")]
    [InlineData(-2, "350.00")]
    [InlineData(1, "-1.00")]
    public async Task ItemPedido_QuantidadeOuPrecoInvalido_EhRecusadoPeloCheck(int quantidade, string precoUnitario)
    {
        var erro = await DeveSerRecusadoAsync($"""
            {InserirCliente()}
            {InserirProduto()}
            {InserirPedido()}
            INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
            VALUES (@pedidoId, @produtoId, {quantidade}, {precoUnitario});
            """);

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
        erro.Message.ShouldContain("CHECK");
    }

    [Fact]
    public async Task Pedido_DeClienteInexistente_EhRecusadoPelaFk()
    {
        var erro = await DeveSerRecusadoAsync(
            "INSERT INTO dbo.Pedidos (ClienteId, Status, Total) VALUES (987654, 'Created', 10.00);");

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
        erro.Message.ShouldContain("FOREIGN KEY");
    }

    [Fact]
    public async Task Cliente_IndicadoPorClienteInexistente_EhRecusadoPelaFk()
    {
        var erro = await DeveSerRecusadoAsync(
            "INSERT INTO dbo.Clientes (Nome, Email, IndicadoPorId) VALUES (N'Bruno', N'bruno@orderflow.dev', 987654);");

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
        erro.Message.ShouldContain("FOREIGN KEY");
    }

    [Fact]
    public async Task ItemPedido_MesmoProdutoDuasVezesNoPedido_EhRecusadoPelaPk()
    {
        var erro = await DeveSerRecusadoAsync($"""
            {InserirCliente()}
            {InserirProduto()}
            {InserirPedido()}
            INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedidoId, @produtoId, 1, 350.00);
            INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedidoId, @produtoId, 3, 350.00);
            """);

        erro.Number.ShouldBe(ViolacaoDeUnique);
        erro.Message.ShouldContain("PRIMARY KEY");
    }

    [Fact]
    public async Task Pedido_Excluido_RemoveOsItensEmCascata()
    {
        var linhas = await EmTransacaoRevertidaAsync($"""
            {InserirCliente()}
            {InserirProduto()}
            {InserirPedido()}
            INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedidoId, @produtoId, 2, 350.00);
            DELETE FROM dbo.Pedidos WHERE Id = @pedidoId;
            SELECT COUNT(*) AS ItensRestantes FROM dbo.ItensPedido WHERE PedidoId = @pedidoId;
            """);

        linhas.Single().Inteiro("ItensRestantes").ShouldBe(0);
    }

    [Fact]
    public async Task Produto_ComVendas_NaoPodeSerExcluido()
    {
        var erro = await DeveSerRecusadoAsync($"""
            {InserirCliente()}
            {InserirProduto()}
            {InserirPedido()}
            INSERT INTO dbo.ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario) VALUES (@pedidoId, @produtoId, 2, 350.00);
            DELETE FROM dbo.Produtos WHERE Id = @produtoId;
            """);

        erro.Number.ShouldBe(ViolacaoDeCheckOuFk);
        erro.Message.ShouldContain("REFERENCE");
    }
}
