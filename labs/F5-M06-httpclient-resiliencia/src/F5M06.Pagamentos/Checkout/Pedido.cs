namespace F5M06.Pagamentos.Checkout;

/// <summary>Situação do pagamento de um pedido do OrderFlow.</summary>
public enum SituacaoPagamento
{
    AguardandoPagamento,
    Pago,
    Recusado,
    /// <summary>Gateway não respondeu: a cobrança pode ou não ter acontecido. Um job reconcilia pelo status.</summary>
    PagamentoPendente,
    /// <summary>O gateway rejeitou a requisição (bug/configuração): precisa de gente olhando.</summary>
    ErroDeIntegracao,
}

/// <summary>Pedido, recortado ao que o checkout precisa.</summary>
public sealed class Pedido(Guid id, decimal total, string moeda = "BRL")
{
    public Guid Id { get; } = id;
    public decimal Total { get; } = total;
    public string Moeda { get; } = moeda;
    public SituacaoPagamento Situacao { get; private set; } = SituacaoPagamento.AguardandoPagamento;
    public string? TransacaoId { get; private set; }

    /// <summary>
    /// Número da tentativa de pagamento. Só avança quando o cartão é RECUSADO: uma nova tentativa
    /// (outro cartão) é outra operação e precisa de outra chave de idempotência.
    /// </summary>
    public int TentativaDePagamento { get; private set; } = 1;

    public void ConfirmarPagamento(string transacaoId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transacaoId);
        TransacaoId = transacaoId;
        Situacao = SituacaoPagamento.Pago;
    }

    public void RegistrarRecusa()
    {
        Situacao = SituacaoPagamento.Recusado;
        TentativaDePagamento++;
    }

    public void MarcarPagamentoPendente() => Situacao = SituacaoPagamento.PagamentoPendente;

    public void MarcarErroDeIntegracao() => Situacao = SituacaoPagamento.ErroDeIntegracao;
}
