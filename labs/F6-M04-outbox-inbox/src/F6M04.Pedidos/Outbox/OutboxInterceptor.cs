using F6M04.Pedidos.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace F6M04.Pedidos.Outbox;

/// <summary>
/// Antes de cada <c>SaveChanges</c>, recolhe os eventos dos agregados rastreados e os grava como linhas de
/// <see cref="OutboxMessage"/> no MESMO <c>DbContext</c> — portanto no mesmo comando/transação do agregado.
/// Ou o pedido E o evento são gravados, ou nenhum dos dois.
/// </summary>
/// <remarks>
/// Vantagem do interceptor: nenhum caso de uso precisa "lembrar" de gravar na Outbox. Desvantagem: é mágica —
/// documente e cubra com teste. A alternativa explícita é fazer isso na Unit of Work/repositório.
/// </remarks>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null) GravarEventosNaOutbox(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null) GravarEventosNaOutbox(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Para cada entidade rastreada que implementa <see cref="ITemEventos"/> e tem eventos: adiciona uma
    /// <see cref="OutboxMessage"/> por evento (use <see cref="OutboxMessage.DoAgregado"/>, que numera as versões) e limpa
    /// os eventos do agregado.
    /// </summary>
    public static void GravarEventosNaOutbox(DbContext contexto)
    {
        ArgumentNullException.ThrowIfNull(contexto);

        // TODO (Passo 1): hoje este método não faz nada (e por isso nenhum evento chega à Outbox).
        //  1. contexto.ChangeTracker.Entries<ITemEventos>() → entidades com Eventos.Count > 0 → .ToList()
        //     (materialize ANTES de adicionar entidades ao ChangeTracker);
        //  2. para cada agregado: contexto.Set<OutboxMessage>().AddRange(OutboxMessage.DoAgregado(agregado));
        //  3. agregado.LimparEventos() (senão um segundo SaveChanges gravaria os mesmos eventos de novo).
        // Não lance exceção aqui: este interceptor roda em TODO SaveChanges, inclusive nos testes de demonstração.
    }
}
