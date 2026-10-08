using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Pedidos.Dominio;
using F4M04.Cqrs.Pedidos.Infra;
using FluentValidation;

namespace F4M04.Cqrs.Pedidos.Escrita;

/// <summary>Command: confirma um pedido (Created → Confirmed).</summary>
public sealed record ConfirmarPedido(Guid PedidoId) : ICommand<Unit>;

public sealed class ConfirmarPedidoValidator : AbstractValidator<ConfirmarPedido>
{
    public ConfirmarPedidoValidator() =>
        RuleFor(c => c.PedidoId).NotEmpty().WithMessage("Informe o pedido.");
}

public sealed class ConfirmarPedidoHandler(
    IRepositorioDePedidos repositorio,
    TimeProvider relogio) : ICommandHandler<ConfirmarPedido, Unit>
{
    /// <summary>
    /// Carrega o agregado (lança <see cref="PedidoNaoEncontradoException"/> se não existir),
    /// chama <see cref="Pedido.Confirmar"/> e devolve <see cref="Unit.Value"/>.
    /// </summary>
    public async Task<Unit> HandleAsync(ConfirmarPedido command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        var pedido = await repositorio.ObterAsync(command.PedidoId, ct)
                     ?? throw new PedidoNaoEncontradoException(command.PedidoId);

        pedido.Confirmar(relogio.GetUtcNow());
        return Unit.Value;
    }
}
