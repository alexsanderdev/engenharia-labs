using F6M05.Sagas.Retry;

namespace F6M05.Sagas.Saga;

/// <summary>PRONTO. Estado da saga de pedido (persistido como <c>tinyint</c>).</summary>
public enum StatusSaga : byte
{
    /// <summary>Pediu <see cref="ReservarEstoque"/>; esperando a resposta do Estoque.</summary>
    AguardandoEstoque = 0,

    /// <summary>Estoque reservado; pediu <see cref="AutorizarPagamento"/>; prazo correndo.</summary>
    AguardandoPagamento = 1,

    /// <summary>Pagamento falhou ou não respondeu; pediu <see cref="LiberarEstoque"/> (compensação).</summary>
    Compensando = 2,

    /// <summary>Terminal feliz: pagamento autorizado, pedido confirmado.</summary>
    Concluida = 3,

    /// <summary>Terminal infeliz: pedido cancelado (com ou sem compensação).</summary>
    Cancelada = 4,
}

/// <summary>PRONTO. Passos já concluídos (flags; persistido como <c>int</c>).</summary>
[Flags]
public enum PassosDaSaga
{
    Nenhum = 0,
    EstoqueReservado = 1,
    PagamentoAutorizado = 2,
    PedidoConfirmado = 4,
    EstoqueLiberado = 8,
    PagamentoEstornado = 16,
    PedidoCancelado = 32,
}

/// <summary>PRONTO. Configuração da saga.</summary>
public sealed record OpcoesDaSaga
{
    /// <summary>Quanto tempo esperar o Pagamento responder antes de compensar.</summary>
    public TimeSpan PrazoDoPagamento { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Quantas vezes o orquestrador recarrega e reaplica após conflito de concorrência.</summary>
    public int TentativasEmConflito { get; init; } = 3;
}

/// <summary>PRONTO. Motivos de cancelamento gravados na saga.</summary>
public static class MotivosDeCancelamento
{
    public const string PrazoDoPagamentoExpirado = "Pagamento não respondeu dentro do prazo";
    public static string PagamentoRecusado(string motivo) => $"Pagamento recusado: {motivo}";
    public static string EstoqueIndisponivel(string motivo) => $"Estoque indisponível: {motivo}";
}

/// <summary>
/// PRONTO. Outra instância atualizou a saga entre a nossa leitura e a nossa escrita
/// (a <see cref="SagaPedido.Versao"/> mudou). É transitório: recarregue e tente de novo.
/// </summary>
public sealed class ConflitoDeConcorrenciaException(Guid pedidoId, int versaoEsperada)
    : ErroTransitorioException($"A saga {pedidoId} não está mais na versão {versaoEsperada}.")
{
    public Guid PedidoId { get; } = pedidoId;
    public int VersaoEsperada { get; } = versaoEsperada;
}

/// <summary>
/// PRONTO. Chegou uma resposta para uma saga que ainda não existe (fora de ordem: a resposta
/// passou na frente do <see cref="PedidoCriado"/>, ou o pedido nunca existiu). É transitório:
/// o retry atrasado dá tempo para o início chegar; se nunca chegar, a DLQ mostra o problema.
/// </summary>
public sealed class SagaNaoEncontradaException(Guid pedidoId, string tipoDaMensagem)
    : ErroTransitorioException($"Saga do pedido {pedidoId} não encontrada para {tipoDaMensagem}.")
{
    public Guid PedidoId { get; } = pedidoId;
}
