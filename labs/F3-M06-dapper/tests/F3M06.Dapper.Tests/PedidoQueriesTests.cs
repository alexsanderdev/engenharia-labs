using F3M06.Dapper.Escrita;
using F3M06.Dapper.Leitura;
using F3M06.Dapper.Tests.Infra;

namespace F3M06.Dapper.Tests;

/// <summary>Passos 4 a 8: multi-mapping, QueryMultiple, relatório agregado e paginação.</summary>
public sealed class PedidoQueriesTests(BancoFixture banco)
{
    private readonly PedidoQueries _queries = new(banco.ConnectionString);
    private DadosDeTeste Dados => banco.Dados;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---------- Passo 4: multi-mapping (um pedido) ----------

    [Fact]
    public async Task ObterComItens_PedidoComDoisItens_MapeiaCabecalhoEItensNumaConsulta()
    {
        var pedido = await _queries.ObterComItensAsync(DadosDeTeste.PedidoAnaComDoisItensId, Ct);

        pedido.ShouldNotBeNull();
        pedido.ClienteId.ShouldBe(DadosDeTeste.AnaId);
        pedido.ClienteNome.ShouldBe("Ana Souza");
        pedido.CriadoEm.ShouldBe(new DateTime(2026, 3, 2, 10, 0, 0));
        pedido.Status.ShouldBe(StatusPedido.Completed);
        pedido.Total.ShouldBe(490.00m);

        pedido.Itens.Count.ShouldBe(2);
        pedido.Itens[0].Sku.ShouldBe("TEC-001");
        pedido.Itens[0].NomeProduto.ShouldBe("Teclado Mecânico");
        pedido.Itens[0].Quantidade.ShouldBe(1);
        pedido.Itens[1].Sku.ShouldBe("MOU-001");
        pedido.Itens[1].ProdutoId.ShouldBe(Dados.Mouse.Id);
        pedido.Itens[1].Subtotal.ShouldBe(240.00m);
        pedido.Itens.Sum(i => i.Subtotal).ShouldBe(pedido.Total);
    }

    [Fact]
    public async Task ObterComItens_PedidoSemItens_VemComListaVazia_EInexistenteVemNull()
    {
        var semItens = await _queries.ObterComItensAsync(DadosDeTeste.PedidoAnaSemItensId, Ct);
        var inexistente = await _queries.ObterComItensAsync(Guid.NewGuid(), Ct);

        semItens.ShouldNotBeNull();
        semItens.Status.ShouldBe(StatusPedido.Created);
        semItens.Itens.ShouldBeEmpty(); // e não uma lista com um item "vazio" vindo do LEFT JOIN
        inexistente.ShouldBeNull();
    }

    // ---------- Passo 5: multi-mapping (vários pedidos) ----------

    [Fact]
    public async Task ListarDoClienteComItens_VariosPedidos_CadaPedidoApareceUmaVezComSeusItens()
    {
        var pedidos = await _queries.ListarDoClienteComItensAsync(DadosDeTeste.BrunoId, Ct);

        var esperados = Dados.Pedidos
            .Where(p => p.ClienteId == DadosDeTeste.BrunoId)
            .OrderByDescending(p => p.CriadoEm)
            .ToList();

        // O JOIN devolve 2 linhas por pedido do Bruno; sem o "identity map" viriam 42 objetos.
        pedidos.Select(p => p.Id).ShouldBe(esperados.Select(p => p.Id));
        pedidos.Select(p => p.Itens.Count).ShouldBe(esperados.Select(p => p.Itens.Count));
        pedidos.Select(p => p.Total).ShouldBe(esperados.Select(p => p.Total));
        pedidos.ShouldAllBe(p => p.Itens.Sum(i => i.Subtotal) == p.Total);
    }

    // ---------- Passo 6: QueryMultiple ----------

    [Fact]
    public async Task ObterPainelDoCliente_ClienteComPedidos_TresResultSetsNumaIdaAoBanco()
    {
        var painel = await _queries.ObterPainelDoClienteAsync(DadosDeTeste.AnaId, ultimos: 3, Ct);

        painel.ShouldNotBeNull();
        painel.Cliente.ShouldBe(new ClienteResumo { Id = DadosDeTeste.AnaId, Nome = "Ana Souza", Email = "ana@orderflow.dev" });

        // Os 3 mais recentes da Ana: 31/03, 06/03 e 05/03.
        painel.UltimosPedidos.Select(p => p.CriadoEm).ShouldBe(
        [
            new DateTime(2026, 3, 31, 23, 59, 59),
            new DateTime(2026, 3, 6, 11, 0, 0),
            new DateTime(2026, 3, 5, 9, 30, 0),
        ]);
        painel.UltimosPedidos.ShouldAllBe(p => p.ClienteNome == "Ana Souza");

        // 5 pedidos, 1 cancelado (1.500) fora da conta: 490 + 120 + 0 + 179,80.
        painel.Estatisticas.QuantidadePedidos.ShouldBe(4);
        painel.Estatisticas.TotalGasto.ShouldBe(789.80m);
        painel.Estatisticas.UltimoPedidoEm.ShouldBe(new DateTime(2026, 3, 31, 23, 59, 59));
    }

    [Fact]
    public async Task ObterPainelDoCliente_ClienteSemPedidos_EstatisticasZeradas_EInexistenteNull()
    {
        var carla = await _queries.ObterPainelDoClienteAsync(DadosDeTeste.CarlaId, ct: Ct);
        var ninguem = await _queries.ObterPainelDoClienteAsync(Guid.NewGuid(), ct: Ct);

        carla.ShouldNotBeNull();
        carla.Cliente.Nome.ShouldBe("Carla Dias");
        carla.UltimosPedidos.ShouldBeEmpty();
        carla.Estatisticas.ShouldBe(new EstatisticasDoCliente { QuantidadePedidos = 0, TotalGasto = 0m, UltimoPedidoEm = null });
        ninguem.ShouldBeNull();
    }

    // ---------- Passo 7: relatório agregado (Sql/RelatorioDeVendas.sql) ----------

    [Fact]
    public async Task RelatorioDeVendas_DiaConhecido_ContaPedidoUmaVezEIgnoraCancelado()
    {
        var relatorio = await _queries.RelatorioDeVendasAsync(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 2), Ct);

        // 02/03: pedido de 490 (2 itens, 3 unidades) + pedido CANCELADO de 1.500.
        // Se o SQL somar p.Total depois do JOIN com itens, o 490 vira 980.
        var dia = relatorio.ShouldHaveSingleItem();
        dia.Dia.ShouldBe(new DateTime(2026, 3, 2));
        dia.QuantidadePedidos.ShouldBe(1);
        dia.ItensVendidos.ShouldBe(3);
        dia.Faturamento.ShouldBe(490.00m);
        dia.TicketMedio.ShouldBe(490.00m);
    }

    [Fact]
    public async Task RelatorioDeVendas_MesInteiro_BateComOCalculoEmMemoriaDiaADia()
    {
        var relatorio = await _queries.RelatorioDeVendasAsync(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), Ct);

        var esperado = Dados.Pedidos
            .Where(p => p.CriadoEm >= new DateTime(2026, 3, 1) && p.CriadoEm < new DateTime(2026, 4, 1))
            .Where(p => p.Status != StatusPedido.Cancelled && p.Itens.Count > 0)
            .GroupBy(p => p.CriadoEm.Date)
            .OrderBy(g => g.Key)
            .Select(g => new VendasPorDia
            {
                Dia = g.Key,
                QuantidadePedidos = g.Count(),
                ItensVendidos = g.Sum(p => p.Itens.Sum(i => i.Quantidade)),
                Faturamento = g.Sum(p => p.Total),
            })
            .ToList();

        relatorio.ShouldBe(esperado);
    }

    [Fact]
    public async Task RelatorioDeVendas_LimitesDoPeriodo_IncluiODiaFinalInteiroEExcluiODiaSeguinte()
    {
        var relatorio = await _queries.RelatorioDeVendasAsync(new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 31), Ct);

        // 31/03 23:59:59 entra (BETWEEN '2026-03-01' AND '2026-03-31' perderia esse pedido);
        // 01/04 00:00:00 não entra.
        var dia = relatorio.ShouldHaveSingleItem();
        dia.Dia.ShouldBe(new DateTime(2026, 3, 31));
        dia.Faturamento.ShouldBe(179.80m);
    }

    // ---------- Passo 8: paginação ----------

    [Fact]
    public async Task ListarPaginado_PrimeiraPagina_MaisRecentesPrimeiroComTotal()
    {
        var pagina = await _queries.ListarPaginadoAsync(1, 10, Ct);

        var esperados = Dados.Pedidos.OrderByDescending(p => p.CriadoEm).Take(10).Select(p => p.Id);
        pagina.Itens.Select(p => p.Id).ShouldBe(esperados);
        pagina.TotalItens.ShouldBe(Dados.Pedidos.Count);
        pagina.TotalPaginas.ShouldBe(3);
        pagina.Itens[0].ClienteNome.ShouldBe("Bruno Lima"); // 01/04 é o mais recente
    }

    [Fact]
    public async Task ListarPaginado_UltimaPaginaParcialEPaginaAlemDoFim()
    {
        var ultima = await _queries.ListarPaginadoAsync(3, 10, Ct);
        var alem = await _queries.ListarPaginadoAsync(4, 10, Ct);

        var esperados = Dados.Pedidos.OrderByDescending(p => p.CriadoEm).Skip(20).Select(p => p.Id);
        ultima.Itens.Select(p => p.Id).ShouldBe(esperados);
        alem.Itens.ShouldBeEmpty();
        alem.TotalItens.ShouldBe(Dados.Pedidos.Count);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, PedidoQueries.TamanhoMaximoPagina + 1)]
    public async Task ListarPaginado_ParametrosInvalidos_LancaArgumentOutOfRange(int numeroPagina, int tamanho)
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _queries.ListarPaginadoAsync(numeroPagina, tamanho, Ct));
    }
}
