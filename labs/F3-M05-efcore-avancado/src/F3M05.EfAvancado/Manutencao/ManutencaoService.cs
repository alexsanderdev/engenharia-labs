using F3M05.EfAvancado.Dominio;
using F3M05.EfAvancado.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace F3M05.EfAvancado.Manutencao;

/// <summary>Rotinas de manutenção em lote (rodadas por um job noturno).</summary>
public sealed class ManutencaoService(LojaDbContext db, TimeProvider relogio)
{
    /// <summary>
    /// Cancela pedidos em <see cref="StatusPedido.Created"/> criados há mais de
    /// <paramref name="idadeMaxima"/>, preenchendo <c>AtualizadoEm</c> com agora.
    /// Meta: UM comando UPDATE, sem carregar entidades. Devolve quantos foram cancelados.
    /// </summary>
    public async Task<int> CancelarAbandonadosAsync(TimeSpan idadeMaxima, CancellationToken ct = default)
    {
        // CONSULTA RUIM: traz TODOS os pedidos abandonados para a memória, rastreia cada um
        // e depois manda os UPDATEs. Com 200 mil pedidos, isso derruba o job.
        // TODO (Passo 8): troque por Where(...).ExecuteUpdateAsync(s => s.SetProperty(...).SetProperty(...)).
        //                 Lembre: ExecuteUpdate NÃO passa pelo interceptador de auditoria.
        var limite = relogio.GetUtcNow() - idadeMaxima;
        var abandonados = await db.Pedidos
            .Where(p => p.Status == StatusPedido.Created && p.CriadoEm < limite)
            .ToListAsync(ct);

        foreach (var pedido in abandonados)
            pedido.Status = StatusPedido.Cancelled;

        await db.SaveChangesAsync(ct);
        return abandonados.Count;
    }

    /// <summary>
    /// Apaga eventos de pedido ocorridos antes de <paramref name="antesDe"/>.
    /// Meta: UM comando DELETE, sem carregar entidades. Devolve quantos foram apagados.
    /// </summary>
    public async Task<int> ExpurgarEventosAsync(DateTimeOffset antesDe, CancellationToken ct = default)
    {
        // CONSULTA RUIM: carrega para depois apagar. TODO (Passo 8): ExecuteDeleteAsync.
        var antigos = await db.EventosPedido.Where(e => e.OcorridoEm < antesDe).ToListAsync(ct);
        db.EventosPedido.RemoveRange(antigos);
        await db.SaveChangesAsync(ct);
        return antigos.Count;
    }
}
