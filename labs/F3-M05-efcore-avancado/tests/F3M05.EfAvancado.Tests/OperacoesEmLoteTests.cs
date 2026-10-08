using F3M05.EfAvancado.Dominio;
using F3M05.EfAvancado.Manutencao;
using F3M05.EfAvancado.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F3M05.EfAvancado.Tests;

/// <summary>Passo 8: ExecuteUpdate/ExecuteDelete — operações em lote sem carregar entidades.</summary>
public sealed class OperacoesEmLoteTests(SqlServerFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task CancelarAbandonadosAsync_UmUpdateSemCarregarEntidades()
    {
        var produto = await SemearProdutoAsync("Garrafa", 25m);
        var antigos = await SemearClienteComPedidosAsync("Júlia", 3, [produto]);
        await using (var db = NovoContexto())
        {
            db.Attach(antigos.Pedidos[2]).Entity.Status = StatusPedido.Confirmed; // confirmado não é abandonado
            await db.SaveChangesAsync(Ct);
        }
        Relogio.Advance(TimeSpan.FromDays(3));
        var recentes = await SemearClienteComPedidosAsync("Kaio", 1, [produto]);
        Relogio.Advance(TimeSpan.FromHours(1));
        Sql.Limpar();

        int cancelados;
        await using (var db = NovoContexto())
        {
            cancelados = await new ManutencaoService(db, Relogio).CancelarAbandonadosAsync(TimeSpan.FromDays(2), Ct);
            db.ChangeTracker.Entries().ShouldBeEmpty("nada deveria ter sido carregado");
        }

        cancelados.ShouldBe(2);
        Sql.Comandos.Count.ShouldBe(1, Sql.Relatorio());
        Sql.Comandos[0].TrimStart().ShouldStartWith("UPDATE", Case.Insensitive);

        await using var verificacao = NovoContexto();
        var status = await verificacao.Pedidos.AsNoTracking()
            .Where(p => p.ClienteId == antigos.Id || p.ClienteId == recentes.Id)
            .OrderBy(p => p.Id)
            .Select(p => new { p.Status, p.AtualizadoEm })
            .ToListAsync(Ct);
        status.Select(s => s.Status).ShouldBe(
            [StatusPedido.Cancelled, StatusPedido.Cancelled, StatusPedido.Confirmed, StatusPedido.Created]);
        status[0].AtualizadoEm.ShouldBe(Relogio.GetUtcNow(), "ExecuteUpdate não passa pelo interceptador: preencha AtualizadoEm no SetProperty");
    }

    [Fact]
    public async Task ExpurgarEventosAsync_UmDeleteSemCarregarEntidades()
    {
        var produto = await SemearProdutoAsync("Toalha", 40m);
        var cliente = await SemearClienteComPedidosAsync("Lia", 2, [produto], eventosPorPedido: 3); // 6 eventos "antigos"
        Relogio.Advance(TimeSpan.FromDays(30));
        await SemearClienteComPedidosAsync("Mel", 1, [produto], eventosPorPedido: 2);           // 2 eventos recentes
        Sql.Limpar();

        int apagados;
        await using (var db = NovoContexto())
            apagados = await new ManutencaoService(db, Relogio).ExpurgarEventosAsync(Relogio.GetUtcNow().AddDays(-7), Ct);

        apagados.ShouldBe(6);
        Sql.Comandos.Count.ShouldBe(1, Sql.Relatorio());
        Sql.Comandos[0].TrimStart().ShouldStartWith("DELETE", Case.Insensitive);
        (await EscalarAsync("SELECT COUNT(*) FROM EventosPedido p JOIN Pedidos x ON x.Id = p.PedidoId WHERE x.ClienteId = @c", ("@c", cliente.Id)))
            .ShouldBe(0);
    }
}
