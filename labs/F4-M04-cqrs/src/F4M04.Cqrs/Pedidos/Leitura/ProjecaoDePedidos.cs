using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Pedidos.Dominio;

namespace F4M04.Cqrs.Pedidos.Leitura;

/// <summary>
/// Projeção: transforma eventos de domínio em linhas do read model.
/// É a ÚNICA classe que escreve no <see cref="BancoDeLeitura"/>.
/// </summary>
public sealed class ProjecaoDePedidos(BancoDeLeitura leitura)
    : IDomainEventHandler<PedidoCriado>, IDomainEventHandler<PedidoConfirmado>
{
    /// <summary>Cria (ou substitui, para ser idempotente) o resumo com status "Created".</summary>
    public Task HandleAsync(PedidoCriado domainEvent, CancellationToken ct)
    {
        _ = leitura;
        throw new NotImplementedException(
            "TODO: grave leitura.Pedidos[PedidoId] = new PedidoResumo(..., Status: \"Created\", CriadoEm: OcorreuEm, ConfirmadoEm: null).");
    }

    /// <summary>Atualiza status e data de confirmação, preservando os demais campos. Pedido desconhecido é ignorado.</summary>
    public Task HandleAsync(PedidoConfirmado domainEvent, CancellationToken ct) =>
        throw new NotImplementedException(
            "TODO: se o resumo existir, substitua por 'atual with { Status = \"Confirmed\", ConfirmadoEm = OcorreuEm }'; senão ignore.");
}
