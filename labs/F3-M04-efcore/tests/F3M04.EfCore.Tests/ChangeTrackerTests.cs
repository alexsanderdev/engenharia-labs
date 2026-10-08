using F3M04.EfCore.Dominio;
using F3M04.EfCore.Pedidos;
using F3M04.EfCore.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F3M04.EfCore.Tests;

/// <summary>
/// Passo 8: o DbContext como unit of work. O change tracker compara o snapshot do que foi
/// carregado com o estado atual e gera SÓ o UPDATE necessário.
/// </summary>
public sealed class ChangeTrackerTests(SqlServerFixture fixture) : BancoTestBase(fixture)
{
    [Fact]
    public async Task EntidadeCarregada_AlterarStatus_MarcaSoAPropriedadeAlterada()
    {
        var cliente = await SemearClienteAsync();
        var produto = await SemearProdutoAsync("Headset", 300m);
        var semeado = await SemearPedidoAsync(cliente, Relogio.GetUtcNow(), (produto, 1));

        await using var db = NovoContexto();
        var pedido = (await new PedidoService(db, Relogio).ObterComItensAsync(semeado.Id, Ct))!;
        db.Entry(pedido).State.ShouldBe(EntityState.Unchanged);

        pedido.Confirmar();
        db.ChangeTracker.DetectChanges();

        var entrada = db.Entry(pedido);
        entrada.State.ShouldBe(EntityState.Modified);
        entrada.Property(p => p.Status).IsModified.ShouldBeTrue();
        entrada.Property(p => p.Status).OriginalValue.ShouldBe(StatusPedido.Created);
        entrada.Property(p => p.Total).IsModified.ShouldBeFalse();
        entrada.Property(p => p.CriadoEm).IsModified.ShouldBeFalse();
        db.Entry(pedido.Itens.Single()).State.ShouldBe(EntityState.Unchanged, "os itens não mudaram");
    }
}
