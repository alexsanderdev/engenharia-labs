using System.Collections.Concurrent;
using F6M05.Sagas.Retry;
using F6M05.Sagas.Saga;

namespace F6M05.Sagas.Coreografia;

/// <summary>
/// Estoque na coreografia: reage a <see cref="PedidoCriado"/> reservando e a
/// <see cref="PagamentoRecusado"/> liberando (a compensação também é uma reação).
/// </summary>
public sealed class EstoqueCoreografado : ServicoCoreografado
{
    public override string Nome => "estoque";

    public override IReadOnlyList<string> Interesses => [nameof(PedidoCriado), nameof(PagamentoRecusado)];

    /// <summary>
    /// <see cref="PedidoCriado"/> → <see cref="EstoqueReservado"/>; <see cref="PagamentoRecusado"/> →
    /// <see cref="EstoqueLiberado"/>; qualquer outro → <c>null</c>. Ids com <see cref="ServicoCoreografado.IdDoEvento{TEvento}"/>.
    /// </summary>
    public override MensagemSaga? Reagir(MensagemSaga evento) =>
        throw new NotImplementedException("TODO: reaja a PedidoCriado (reservar) e a PagamentoRecusado (liberar) (Passo 10).");
}

/// <summary>
/// Pagamento na coreografia: precisa do VALOR do pedido, mas reage a <see cref="EstoqueReservado"/>,
/// que não traz valor. Então guarda o que aprendeu com <see cref="PedidoCriado"/> (estado local de
/// cada serviço: é assim que a coreografia "lembra" das coisas).
/// </summary>
/// <param name="limiteDeAprovacao">Pedidos acima deste valor são recusados (regra do fake).</param>
public sealed class PagamentoCoreografado(decimal limiteDeAprovacao) : ServicoCoreografado
{
    /// <summary>PRONTO. Estado local do Pagamento: valor de cada pedido visto em PedidoCriado.</summary>
    private readonly ConcurrentDictionary<Guid, decimal> _valores = new();

    public override string Nome => "pagamento";

    public override IReadOnlyList<string> Interesses => [nameof(PedidoCriado), nameof(EstoqueReservado)];

    /// <summary>
    /// <see cref="PedidoCriado"/> → guarda o valor e devolve <c>null</c>.
    /// <see cref="EstoqueReservado"/> → valor ≤ limite: <see cref="PagamentoAutorizado"/> (autorização
    /// <c>aut-{pedidoId:N}</c>); acima: <see cref="PagamentoRecusado"/> ("limite excedido"). Se o valor
    /// ainda não é conhecido (fora de ordem), lança <see cref="ErroTransitorioException"/>.
    /// </summary>
    public override MensagemSaga? Reagir(MensagemSaga evento) =>
        throw new NotImplementedException("TODO: guarde o valor do PedidoCriado e decida o pagamento no EstoqueReservado (Passo 10).");
}
