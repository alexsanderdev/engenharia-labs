using System.Text.Json.Serialization;

namespace F6M05.Sagas.Saga;

/// <summary>PRONTO. Item do pedido (o suficiente para reservar estoque).</summary>
public sealed record ItemDoPedido(Guid ProdutoId, int Quantidade);

/// <summary>
/// PRONTO. Mensagens que ENTRAM na saga (eventos dos participantes e o prazo interno).
/// Todas carregam <see cref="MessageId"/> (idempotência) e <see cref="PedidoId"/> (correlação:
/// identifica a instância da saga).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$tipo")]
[JsonDerivedType(typeof(PedidoCriado), nameof(PedidoCriado))]
[JsonDerivedType(typeof(EstoqueReservado), nameof(EstoqueReservado))]
[JsonDerivedType(typeof(EstoqueIndisponivel), nameof(EstoqueIndisponivel))]
[JsonDerivedType(typeof(PagamentoAutorizado), nameof(PagamentoAutorizado))]
[JsonDerivedType(typeof(PagamentoRecusado), nameof(PagamentoRecusado))]
[JsonDerivedType(typeof(EstoqueLiberado), nameof(EstoqueLiberado))]
[JsonDerivedType(typeof(PrazoDoPagamentoExpirado), nameof(PrazoDoPagamentoExpirado))]
public abstract record MensagemSaga(string MessageId, Guid PedidoId)
{
    /// <summary>Nome do tipo (vai na propriedade AMQP <c>type</c> e na routing key).</summary>
    [JsonIgnore]
    public string Tipo => GetType().Name;
}

/// <summary>O módulo de Pedidos aceitou um pedido: começa a saga.</summary>
public sealed record PedidoCriado(string MessageId, Guid PedidoId, Guid ClienteId, decimal Valor, IReadOnlyList<ItemDoPedido> Itens)
    : MensagemSaga(MessageId, PedidoId);

/// <summary>Resposta do Estoque a <see cref="ReservarEstoque"/>: reservado.</summary>
public sealed record EstoqueReservado(string MessageId, Guid PedidoId) : MensagemSaga(MessageId, PedidoId);

/// <summary>Resposta do Estoque a <see cref="ReservarEstoque"/>: não há saldo (nada foi reservado).</summary>
public sealed record EstoqueIndisponivel(string MessageId, Guid PedidoId, string Motivo) : MensagemSaga(MessageId, PedidoId);

/// <summary>Resposta do Pagamento a <see cref="AutorizarPagamento"/>: autorizado.</summary>
public sealed record PagamentoAutorizado(string MessageId, Guid PedidoId, string AutorizacaoId) : MensagemSaga(MessageId, PedidoId);

/// <summary>Resposta do Pagamento a <see cref="AutorizarPagamento"/>: recusado.</summary>
public sealed record PagamentoRecusado(string MessageId, Guid PedidoId, string Motivo) : MensagemSaga(MessageId, PedidoId);

/// <summary>Resposta do Estoque à compensação <see cref="LiberarEstoque"/>.</summary>
public sealed record EstoqueLiberado(string MessageId, Guid PedidoId) : MensagemSaga(MessageId, PedidoId);

/// <summary>Mensagem interna: o prazo para o Pagamento responder venceu (gerada pelo <see cref="VerificadorDePrazos"/>).</summary>
public sealed record PrazoDoPagamentoExpirado(string MessageId, Guid PedidoId) : MensagemSaga(MessageId, PedidoId);

/// <summary>
/// PRONTO. Comandos que SAEM da saga para os participantes. O <see cref="MessageId"/> é
/// DETERMINÍSTICO (<c>{pedidoId:N}:{Tipo}</c>): republicar o mesmo comando gera o mesmo id, e o
/// participante deduplica. É isso que torna seguro reenviar comandos depois de uma falha.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$tipo")]
[JsonDerivedType(typeof(ReservarEstoque), nameof(ReservarEstoque))]
[JsonDerivedType(typeof(AutorizarPagamento), nameof(AutorizarPagamento))]
[JsonDerivedType(typeof(LiberarEstoque), nameof(LiberarEstoque))]
[JsonDerivedType(typeof(EstornarPagamento), nameof(EstornarPagamento))]
[JsonDerivedType(typeof(ConfirmarPedido), nameof(ConfirmarPedido))]
[JsonDerivedType(typeof(CancelarPedido), nameof(CancelarPedido))]
public abstract record ComandoSaga(string MessageId, Guid PedidoId)
{
    [JsonIgnore]
    public string Tipo => GetType().Name;

    /// <summary>Id determinístico de um comando da saga.</summary>
    public static string IdPara<TComando>(Guid pedidoId) where TComando : ComandoSaga =>
        $"{pedidoId:N}:{typeof(TComando).Name}";
}

public sealed record ReservarEstoque(string MessageId, Guid PedidoId, IReadOnlyList<ItemDoPedido> Itens) : ComandoSaga(MessageId, PedidoId);

public sealed record AutorizarPagamento(string MessageId, Guid PedidoId, Guid ClienteId, decimal Valor) : ComandoSaga(MessageId, PedidoId);

/// <summary>Compensação da reserva de estoque.</summary>
public sealed record LiberarEstoque(string MessageId, Guid PedidoId) : ComandoSaga(MessageId, PedidoId);

/// <summary>Compensação de uma autorização de pagamento que chegou tarde demais.</summary>
public sealed record EstornarPagamento(string MessageId, Guid PedidoId, string AutorizacaoId) : ComandoSaga(MessageId, PedidoId);

public sealed record ConfirmarPedido(string MessageId, Guid PedidoId) : ComandoSaga(MessageId, PedidoId);

public sealed record CancelarPedido(string MessageId, Guid PedidoId, string Motivo) : ComandoSaga(MessageId, PedidoId);
