using System.Collections.Concurrent;
using F6M03.Kafka.Contratos;

namespace F6M03.Kafka.Consumo;

/// <summary>Regra de negócio do consumidor (ex.: enviar notificação, reservar estoque).</summary>
public interface IManipuladorDeEvento
{
    Task ManipularAsync(IEventoDePedido evento, CancellationToken ct);
}

/// <summary>Guarda os <c>EventoId</c> já processados por um consumidor.</summary>
public interface IRegistroDeEventosProcessados
{
    /// <summary>
    /// Tenta registrar o evento. Devolve <c>true</c> se é a primeira vez (pode processar)
    /// e <c>false</c> se já foi registrado antes (duplicata).
    /// </summary>
    Task<bool> TentarRegistrarAsync(Guid eventoId, CancellationToken ct);

    /// <summary>Desfaz o registro (o processamento falhou e o evento precisa poder ser reprocessado).</summary>
    Task RemoverAsync(Guid eventoId, CancellationToken ct);
}

/// <summary>
/// Registro em memória (pronto). Em produção isto é a tabela de Inbox no MESMO banco e na MESMA
/// transação do efeito colateral — é o assunto do módulo 6.04 (Outbox, Inbox e Idempotência).
/// </summary>
public sealed class RegistroEmMemoria : IRegistroDeEventosProcessados
{
    private readonly ConcurrentDictionary<Guid, byte> _ids = new();

    public int Quantidade => _ids.Count;

    public Task<bool> TentarRegistrarAsync(Guid eventoId, CancellationToken ct) =>
        Task.FromResult(_ids.TryAdd(eventoId, 0));

    public Task RemoverAsync(Guid eventoId, CancellationToken ct)
    {
        _ids.TryRemove(eventoId, out _);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Decorator que torna qualquer manipulador idempotente: o mesmo <c>EventoId</c> só produz efeito uma vez,
/// mesmo que o Kafka entregue a mensagem de novo (at-least-once, rebalance, producer reenviando).
/// </summary>
public sealed class ManipuladorIdempotente(IManipuladorDeEvento interno, IRegistroDeEventosProcessados registro)
    : IManipuladorDeEvento
{
    /// <summary>
    /// Passo 6: se <see cref="IRegistroDeEventosProcessados.TentarRegistrarAsync"/> devolver <c>false</c>, ignore
    /// (duplicata). Senão, chame o <paramref name="interno"/>; se ele lançar, remova o registro
    /// (<see cref="IRegistroDeEventosProcessados.RemoverAsync"/>) e relance — a próxima entrega precisa poder processar.
    /// </summary>
    public Task ManipularAsync(IEventoDePedido evento, CancellationToken ct) =>
        throw new NotImplementedException(
            "TODO (Passo 6): se registro.TentarRegistrarAsync(evento.EventoId) devolver false, retorne (duplicata); " +
            "senão chame interno.ManipularAsync e, se ele lançar, registro.RemoverAsync + throw.");
}
