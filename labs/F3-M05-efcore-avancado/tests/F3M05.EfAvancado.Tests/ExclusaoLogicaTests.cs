using F3M05.EfAvancado.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F3M05.EfAvancado.Tests;

/// <summary>Passo 3: soft delete com interceptador + global query filter.</summary>
public sealed class ExclusaoLogicaTests(SqlServerFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task RemoverPedido_ViraUpdateDeExcluido_LinhaContinuaNoBanco()
    {
        var produto = await SemearProdutoAsync("Caneta", 5m);
        var cliente = await SemearClienteComPedidosAsync("Ana", 1, [produto]);
        var pedidoId = cliente.Pedidos[0].Id;
        Relogio.Advance(TimeSpan.FromMinutes(5));
        Sql.Limpar();

        await using (var db = NovoContexto())
        {
            var pedido = await db.Pedidos.SingleAsync(p => p.Id == pedidoId, Ct);
            db.Pedidos.Remove(pedido);
            await db.SaveChangesAsync(Ct);
        }

        Sql.Comandos.ShouldNotContain(c => c.Contains("DELETE", StringComparison.OrdinalIgnoreCase), Sql.Relatorio());
        var linha = (await LinhasAsync("SELECT Excluido, AtualizadoEm FROM Pedidos WHERE Id = @id", ("@id", pedidoId))).Single();
        linha["Excluido"].ShouldBe(true);
        linha["AtualizadoEm"].ShouldBe(Relogio.GetUtcNow());
        (await EscalarAsync("SELECT COUNT(*) FROM ItensPedido WHERE PedidoId = @id", ("@id", pedidoId))).ShouldBe(1, "os itens continuam lá");
    }

    [Fact]
    public async Task Consultas_FiltroGlobal_EscondeExcluidosAteViaInclude()
    {
        var produto = await SemearProdutoAsync("Lápis", 2m);
        var cliente = await SemearClienteComPedidosAsync("Bia", 3, [produto]);
        await using (var db = NovoContexto())
        {
            db.Pedidos.Remove(await db.Pedidos.SingleAsync(p => p.Id == cliente.Pedidos[1].Id, Ct));
            await db.SaveChangesAsync(Ct);
        }

        await using var leitura = NovoContexto();
        (await leitura.Pedidos.CountAsync(p => p.ClienteId == cliente.Id, Ct)).ShouldBe(2);

        var comPedidos = await leitura.Clientes.AsNoTracking().Include(c => c.Pedidos).SingleAsync(c => c.Id == cliente.Id, Ct);
        comPedidos.Pedidos.Count.ShouldBe(2, "o filtro vale também para navegações");

        (await leitura.Pedidos.IgnoreQueryFilters().CountAsync(p => p.ClienteId == cliente.Id, Ct))
            .ShouldBe(3, "IgnoreQueryFilters é a saída explícita (auditoria, admin)");
    }
}
