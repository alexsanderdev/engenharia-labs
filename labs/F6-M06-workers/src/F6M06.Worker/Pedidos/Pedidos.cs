using System.Collections.Concurrent;

namespace F6M06.Worker.Pedidos;

/// <summary>Status do pedido no OrderFlow. (PRONTO)</summary>
public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}

/// <summary>
/// Pedido (recorte do OrderFlow). Regras: Created → Confirmed → Completed; Created → Cancelled;
/// Confirmed e Completed não cancelam. (PRONTO)
/// </summary>
public sealed class Pedido(Guid id, DateTimeOffset criadoEm)
{
    public Guid Id { get; } = id;
    public DateTimeOffset CriadoEm { get; } = criadoEm;
    public StatusPedido Status { get; private set; } = StatusPedido.Created;
    public string? MotivoCancelamento { get; private set; }

    public void Confirmar()
    {
        if (Status != StatusPedido.Created)
            throw new InvalidOperationException($"Pedido {Id} em {Status} não pode ser confirmado.");
        Status = StatusPedido.Confirmed;
    }

    public void Cancelar(string motivo)
    {
        if (Status != StatusPedido.Created)
            throw new InvalidOperationException($"Pedido {Id} em {Status} não pode ser cancelado.");
        Status = StatusPedido.Cancelled;
        MotivoCancelamento = motivo;
    }
}

/// <summary>Repositório de pedidos — SCOPED (como um DbContext). (PRONTO)</summary>
public interface IRepositorioDePedidos
{
    /// <summary>Pedidos ainda em <see cref="StatusPedido.Created"/> criados em ou antes de <paramref name="limite"/>.</summary>
    Task<IReadOnlyList<Pedido>> ListarCriadosAteAsync(DateTimeOffset limite, CancellationToken ct);

    /// <summary>Persiste as alterações (no fake em memória, não faz nada).</summary>
    Task SalvarAsync(CancellationToken ct);
}

/// <summary>"Banco" em memória — SINGLETON, compartilhado entre os escopos. (PRONTO)</summary>
public sealed class BancoEmMemoria
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public void Adicionar(Pedido pedido) => _pedidos[pedido.Id] = pedido;

    public Pedido Obter(Guid id) => _pedidos[id];

    public IReadOnlyList<Pedido> Todos() => [.. _pedidos.Values];
}

/// <summary>Implementação scoped sobre o <see cref="BancoEmMemoria"/>. (PRONTO)</summary>
public sealed class RepositorioDePedidosEmMemoria(BancoEmMemoria banco) : IRepositorioDePedidos
{
    public Task<IReadOnlyList<Pedido>> ListarCriadosAteAsync(DateTimeOffset limite, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Pedido>>(
            [.. banco.Todos().Where(p => p.Status == StatusPedido.Created && p.CriadoEm <= limite)]);

    public Task SalvarAsync(CancellationToken ct) => Task.CompletedTask;
}
