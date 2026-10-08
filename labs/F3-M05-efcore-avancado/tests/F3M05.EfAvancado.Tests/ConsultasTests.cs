using F3M05.EfAvancado.Consultas;
using F3M05.EfAvancado.Dominio;
using F3M05.EfAvancado.Tests.Infra;

namespace F3M05.EfAvancado.Tests;

/// <summary>
/// Passos 4 a 7: consultas que começam RUINS no starter (dão o resultado certo, mas com
/// SQL demais). Cada teste mede os comandos enviados ao banco.
/// </summary>
public sealed class ConsultasTests(SqlServerFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task ListarResumosAsync_SemNMais1_UmUnicoComando()
    {
        var a = await SemearProdutoAsync("Caderno", 20m);
        var b = await SemearProdutoAsync("Borracha", 3m);
        await SemearClienteComPedidosAsync("Carla", 3, [a, b]);
        await SemearClienteComPedidosAsync("Davi", 2, [a]);
        Sql.Limpar();

        await using var db = NovoContexto();
        var resumos = await new RelatorioPedidos(db).ListarResumosAsync(Ct);

        resumos.Count.ShouldBe(5);
        resumos.Select(r => r.ClienteNome).ShouldBe(["Carla", "Carla", "Carla", "Davi", "Davi"]);
        resumos[0].QuantidadeDeItens.ShouldBe(2);
        resumos[0].Total.ShouldBe(50m);  // 1 × 10 + 2 × 20
        resumos[4].Total.ShouldBe(10m);
        Sql.Comandos.Count.ShouldBe(1, $"N+1: 1 consulta + 2 por pedido. Comandos:{Environment.NewLine}{Sql.Relatorio()}");
    }

    [Fact]
    public async Task ObterDoClienteComDetalhesAsync_DuasColecoes_SplitQuerySemProdutoCartesiano()
    {
        var produtos = new List<Produto>();
        for (var i = 0; i < 4; i++)
            produtos.Add(await SemearProdutoAsync($"Item {i}", 1m));
        var cliente = await SemearClienteComPedidosAsync("Edu", 2, produtos, eventosPorPedido: 5);
        Sql.Limpar();

        await using var db = NovoContexto();
        var pedidos = await new PedidoConsultas(db).ObterDoClienteComDetalhesAsync(cliente.Id, Ct);

        pedidos.Count.ShouldBe(2);
        pedidos.ShouldAllBe(p => p.Itens.Count == 4 && p.Eventos.Count == 5);
        Sql.Comandos.Count.ShouldBe(3, $"esperado: pedidos, itens e eventos em comandos separados.{Environment.NewLine}{Sql.Relatorio()}");
        Sql.Comandos.ShouldNotContain(
            c => c.Contains("[ItensPedido]", StringComparison.Ordinal) && c.Contains("[EventosPedido]", StringComparison.Ordinal),
            "um JOIN com as duas coleções devolve itens × eventos linhas por pedido");
    }

    [Fact]
    public async Task ListarItensDoClienteAsync_MesmoProduto_MesmaInstanciaSemTracking()
    {
        var cafe = await SemearProdutoAsync("Café", 30m);
        var acucar = await SemearProdutoAsync("Açúcar", 6m);
        var cliente = await SemearClienteComPedidosAsync("Fabi", 3, [cafe, acucar]);
        Sql.Limpar();

        await using var db = NovoContexto();
        var itens = await new PedidoConsultas(db).ListarItensDoClienteAsync(cliente.Id, Ct);

        itens.Count.ShouldBe(6);
        var dosCafes = itens.Where(i => i.ProdutoId == cafe.Id).Select(i => i.Produto).ToList();
        dosCafes.Count.ShouldBe(3);
        dosCafes.Distinct(ReferenceEqualityComparer.Instance).Count()
            .ShouldBe(1, "com identity resolution, o mesmo produto vira UMA instância em memória");
        db.ChangeTracker.Entries().ShouldBeEmpty("continua sendo uma consulta sem tracking");
        Sql.Comandos.Count.ShouldBe(1);
    }

    [Fact]
    public async Task PedidoComItensPorId_ConsultaCompilada_ResultadoEUmComando()
    {
        var produto = await SemearProdutoAsync("Fone", 120m);
        var cliente = await SemearClienteComPedidosAsync("Gil", 2, [produto]);
        Sql.Limpar();

        await using var db = NovoContexto();
        var pedido = await ConsultasCompiladas.PedidoComItensPorId(db, cliente.Pedidos[1].Id, Ct);
        var inexistente = await ConsultasCompiladas.PedidoComItensPorId(db, -1, Ct);

        pedido.ShouldNotBeNull();
        pedido.Id.ShouldBe(cliente.Pedidos[1].Id);
        pedido.Itens.Count.ShouldBe(1);
        inexistente.ShouldBeNull();
        Sql.Comandos.Count.ShouldBe(2, "um comando por chamada");
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public void PedidoComItensPorId_CriadaComEfCompileAsyncQuery()
    {
        // EF.CompileAsyncQuery devolve um delegate ligado a um objeto interno do EF (CompiledAsync...Query).
        var alvo = ConsultasCompiladas.PedidoComItensPorId.Target;

        alvo.ShouldNotBeNull();
        alvo.GetType().Name.ShouldStartWith("CompiledAsync", Case.Sensitive, "use EF.CompileAsyncQuery, não um lambda comum");
    }

    [Fact]
    public async Task FaturamentoPorDiaAsync_SqlQueryComGroupBy_UmComandoAgregadoNoBanco()
    {
        var produto = await SemearProdutoAsync("Papel", 10m);
        Relogio.Advance(TimeSpan.FromDays(1));
        var inicio = Relogio.GetUtcNow();
        var dia1 = DateOnly.FromDateTime(inicio.UtcDateTime);
        await SemearClienteComPedidosAsync("Hugo", 2, [produto]);   // 2 pedidos × 10 no dia 1
        Relogio.Advance(TimeSpan.FromDays(1));
        var cliente = await SemearClienteComPedidosAsync("Iara", 3, [produto]); // 3 pedidos × 10 no dia 2
        await using (var db = NovoContexto())
        {
            db.Pedidos.Remove(cliente.Pedidos[0]); // excluído não entra
            db.Attach(cliente.Pedidos[1]).Entity.Status = StatusPedido.Cancelled; // cancelado não entra
            await db.SaveChangesAsync(Ct);
        }
        Sql.Limpar();

        await using var leitura = NovoContexto();
        var faturamento = await new RelatorioPedidos(leitura).FaturamentoPorDiaAsync(inicio, Ct);

        faturamento.ShouldBe(
        [
            new FaturamentoDiario(dia1, 2, 20m),
            new FaturamentoDiario(dia1.AddDays(1), 1, 10m),
        ]);
        Sql.Comandos.Count.ShouldBe(1);
        Sql.Comandos[0].ShouldContain("GROUP BY", Case.Insensitive, "a agregação tem de acontecer no banco");
    }
}
