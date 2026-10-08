using F3M04.EfCore.Dominio;
using F3M04.EfCore.Pedidos;
using F3M04.EfCore.Persistencia;
using F3M04.EfCore.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F3M04.EfCore.Tests;

/// <summary>
/// Passos 6 a 8: consultas e escritas do <see cref="PedidoService"/> contra o SQL Server real.
/// Regra de ouro dos asserts: verificar com um DbContext NOVO (o que gravou lembra da memória).
/// </summary>
public sealed class PedidoServiceTests(SqlServerFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task ListarProdutosAtivosAsync_SoAtivosOrdenadosPorNome_SemTracking()
    {
        await SemearProdutoAsync("Zíper inativo", 1m, ativo: false);
        await SemearProdutoAsync("Agenda 2027", 40m);

        await using var db = NovoContexto();
        var produtos = await new PedidoService(db, Relogio).ListarProdutosAtivosAsync(Ct);

        produtos.Select(p => p.Nome).ShouldBe(["Agenda 2027", "Caderno 96 folhas", "Caneta azul", "Mochila executiva"]);
        db.ChangeTracker.Entries().ShouldBeEmpty("leitura para tela não deve encher o change tracker");
    }

    [Fact]
    public async Task CriarAsync_ItensValidos_PersisteComTotalDoServidorEDataDoRelogio()
    {
        var cliente = await SemearClienteAsync();
        var mouse = await SemearProdutoAsync("Mouse", 80m);

        Guid id;
        await using (var db = NovoContexto())
        {
            id = await new PedidoService(db, Relogio).CriarAsync(
                cliente.Id,
                [new ItemSolicitado(mouse.Id, 2), new ItemSolicitado(CatalogoInicial.CanetaId, 4)],
                Ct);

            // Depois do SaveChanges, o que era Added vira Unchanged (o contexto segue rastreando).
            db.ChangeTracker.Entries<Pedido>().Single().State.ShouldBe(EntityState.Unchanged);
        }

        await using var verificacao = NovoContexto();
        var salvo = await verificacao.Pedidos.AsNoTracking().Include(p => p.Itens).SingleAsync(p => p.Id == id, Ct);
        salvo.ClienteId.ShouldBe(cliente.Id);
        salvo.Total.ShouldBe(174.00m); // 2 × 80,00 + 4 × 3,50
        salvo.CriadoEm.ShouldBe(Relogio.GetUtcNow());
        salvo.Status.ShouldBe(StatusPedido.Created);
        salvo.Itens.Count.ShouldBe(2);
        (await EscalarAsync("SELECT Status FROM Pedidos WHERE Id = @id", ("@id", id))).ShouldBe("Created", "status gravado como texto");
    }

    [Fact]
    public async Task CriarAsync_ProdutoInativoOuInexistente_LancaENaoGravaNada()
    {
        var cliente = await SemearClienteAsync();
        var inativo = await SemearProdutoAsync("Fora de linha", 10m, ativo: false);

        await using (var db = NovoContexto())
        {
            var servico = new PedidoService(db, Relogio);
            await Should.ThrowAsync<InvalidOperationException>(() =>
                servico.CriarAsync(cliente.Id, [new ItemSolicitado(inativo.Id, 1)], Ct));

            var erro = await Should.ThrowAsync<InvalidOperationException>(() =>
                servico.CriarAsync(cliente.Id, [new ItemSolicitado(Guid.NewGuid(), 1)], Ct));
            erro.Message.ShouldContain("inexistente");
        }

        await using var verificacao = NovoContexto();
        (await verificacao.Pedidos.CountAsync(p => p.ClienteId == cliente.Id, Ct)).ShouldBe(0);
    }

    [Fact]
    public async Task ObterComItensAsync_ContextoNovo_TrazItensViaIncludeERastreado()
    {
        var cliente = await SemearClienteAsync();
        var a = await SemearProdutoAsync("Monitor", 900m);
        var b = await SemearProdutoAsync("Cabo HDMI", 30m);
        var pedido = await SemearPedidoAsync(cliente, Relogio.GetUtcNow(), (a, 1), (b, 2));

        await using var db = NovoContexto();
        var servico = new PedidoService(db, Relogio);
        var carregado = await servico.ObterComItensAsync(pedido.Id, Ct);

        carregado.ShouldNotBeNull();
        carregado.Itens.Count.ShouldBe(2, "sem Include, os itens não vêm (e não há lazy loading aqui)");
        db.Entry(carregado).State.ShouldBe(EntityState.Unchanged, "para alterar e salvar, a entidade precisa estar rastreada");
        (await servico.ObterComItensAsync(Guid.NewGuid(), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task ObterResumoAsync_ProjecaoParaDto_ComNomesESemTracking()
    {
        var cliente = await SemearClienteAsync("Bruna");
        var cadeira = await SemearProdutoAsync("Cadeira", 700m);
        var pedido = await SemearPedidoAsync(cliente, Relogio.GetUtcNow(), (cadeira, 2), (CatalogoInicial.Produtos[0], 10));

        await using var db = NovoContexto();
        var servico = new PedidoService(db, Relogio);
        var resumo = await servico.ObterResumoAsync(pedido.Id, Ct);

        resumo.ShouldNotBeNull();
        resumo.ClienteNome.ShouldBe("Bruna");
        resumo.Status.ShouldBe(StatusPedido.Created);
        resumo.Total.ShouldBe(1435.00m);
        resumo.Itens.ShouldBe(
        [
            new ItemResumoDto("Cadeira", 2, 700m, 1400m),
            new ItemResumoDto("Caneta azul", 10, 3.50m, 35m),
        ]);
        db.ChangeTracker.Entries().ShouldBeEmpty("projeção para DTO não materializa entidades");
        (await servico.ObterResumoAsync(Guid.NewGuid(), Ct)).ShouldBeNull();
    }

    [Fact]
    public async Task ListarDoClienteAsync_MaisRecentePrimeiro_ComQuantidadeDeItens()
    {
        var cliente = await SemearClienteAsync();
        var outro = await SemearClienteAsync("Outro");
        var produto = await SemearProdutoAsync("Papel A4", 30m);
        var antigo = await SemearPedidoAsync(cliente, Relogio.GetUtcNow().AddDays(-2), (produto, 1));
        var recente = await SemearPedidoAsync(cliente, Relogio.GetUtcNow(), (produto, 3), (CatalogoInicial.Produtos[1], 1));
        await SemearPedidoAsync(outro, Relogio.GetUtcNow(), (produto, 1));

        await using var db = NovoContexto();
        var lista = await new PedidoService(db, Relogio).ListarDoClienteAsync(cliente.Id, Ct);

        lista.Select(p => p.Id).ShouldBe([recente.Id, antigo.Id]);
        lista[0].QuantidadeDeItens.ShouldBe(2);
        lista[0].Total.ShouldBe(114.90m);
        db.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task ConfirmarAsync_PedidoCriado_GravaConfirmedNoBanco()
    {
        var cliente = await SemearClienteAsync();
        var produto = await SemearProdutoAsync("Webcam", 250m);
        var pedido = await SemearPedidoAsync(cliente, Relogio.GetUtcNow(), (produto, 1));

        await using (var db = NovoContexto())
        {
            var servico = new PedidoService(db, Relogio);
            await servico.ConfirmarAsync(pedido.Id, Ct);
            await Should.ThrowAsync<KeyNotFoundException>(() => servico.ConfirmarAsync(Guid.NewGuid(), Ct));
        }

        (await EscalarAsync("SELECT Status FROM Pedidos WHERE Id = @id", ("@id", pedido.Id))).ShouldBe("Confirmed");
    }
}
