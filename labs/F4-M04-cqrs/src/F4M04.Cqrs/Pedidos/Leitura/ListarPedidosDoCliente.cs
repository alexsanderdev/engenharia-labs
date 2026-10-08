using F4M04.Cqrs.Abstractions;
using FluentValidation;

namespace F4M04.Cqrs.Pedidos.Leitura;

/// <summary>Query: pedidos de um cliente, mais recentes primeiro.</summary>
public sealed record ListarPedidosDoCliente(Guid ClienteId) : IQuery<IReadOnlyList<PedidoResumo>>;

/// <summary>Queries também passam pela validação do pipeline.</summary>
public sealed class ListarPedidosDoClienteValidator : AbstractValidator<ListarPedidosDoCliente>
{
    public ListarPedidosDoClienteValidator() =>
        RuleFor(q => q.ClienteId).NotEmpty().WithMessage("Informe o cliente.");
}

public sealed class ListarPedidosDoClienteHandler(BancoDeLeitura leitura)
    : IQueryHandler<ListarPedidosDoCliente, IReadOnlyList<PedidoResumo>>
{
    public Task<IReadOnlyList<PedidoResumo>> HandleAsync(ListarPedidosDoCliente query, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        IReadOnlyList<PedidoResumo> resultado = leitura.Pedidos.Values
            .Where(p => p.ClienteId == query.ClienteId)
            .OrderByDescending(p => p.CriadoEm)
            .ThenByDescending(p => p.Id)
            .ToList();
        return Task.FromResult(resultado);
    }
}
