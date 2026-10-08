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
    public Task<Unit> HandleAsync(ConfirmarPedido command, CancellationToken ct)
    {
        _ = (repositorio, relogio);
        throw new NotImplementedException(
            "TODO: carregue o pedido pelo repositório (null → PedidoNaoEncontradoException), chame Confirmar(relogio.GetUtcNow()) e devolva Unit.Value.");
    }
}
