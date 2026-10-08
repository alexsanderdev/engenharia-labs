using F4M04.Cqrs.Abstractions;
using FluentValidation;

namespace F4M04.Cqrs.Pedidos.Leitura;

/// <summary>Query: pedidos de um cliente, mais recentes primeiro.</summary>
public sealed record ListarPedidosDoCliente(Guid ClienteId) : IQuery<IReadOnlyList<PedidoResumo>>;

/// <summary>Queries também passam pela validação do pipeline.</summary>
public sealed class ListarPedidosDoClienteValidator : AbstractValidator<ListarPedidosDoCliente>
{
    public ListarPedidosDoClienteValidator()
    {
        // TODO: ClienteId não pode ser vazio (mensagem: "Informe o cliente."). Veja o CriarPedidoValidator.
    }
}

/// <summary>
/// Lista do read model os pedidos do cliente, ordenados por CriadoEm decrescente.
/// TODO: receba no construtor apenas o que o lado de leitura precisa.
/// </summary>
public sealed class ListarPedidosDoClienteHandler : IQueryHandler<ListarPedidosDoCliente, IReadOnlyList<PedidoResumo>>
{
    public Task<IReadOnlyList<PedidoResumo>> HandleAsync(ListarPedidosDoCliente query, CancellationToken ct) =>
        throw new NotImplementedException(
            "TODO: injete o BancoDeLeitura, filtre por ClienteId e ordene por CriadoEm decrescente.");
}
