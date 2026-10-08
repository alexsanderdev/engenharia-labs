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
    public Task<int> CancelarAbandonadosAsync(TimeSpan idadeMaxima, CancellationToken ct = default)
    {
        var agora = relogio.GetUtcNow();
        var limite = agora - idadeMaxima;

        // ExecuteUpdate não passa pelo change tracker nem pelos SaveChangesInterceptors:
        // a auditoria (AtualizadoEm) precisa ser feita aqui, explicitamente.
        return db.Pedidos
            .Where(p => p.Status == StatusPedido.Created && p.CriadoEm < limite)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, StatusPedido.Cancelled)
                .SetProperty(p => p.AtualizadoEm, agora), ct);
    }

    /// <summary>
    /// Apaga eventos de pedido ocorridos antes de <paramref name="antesDe"/>.
    /// Meta: UM comando DELETE, sem carregar entidades. Devolve quantos foram apagados.
    /// </summary>
    public Task<int> ExpurgarEventosAsync(DateTimeOffset antesDe, CancellationToken ct = default)
    {
        return db.EventosPedido
            .Where(e => e.OcorridoEm < antesDe)
            .ExecuteDeleteAsync(ct);
    }
}
