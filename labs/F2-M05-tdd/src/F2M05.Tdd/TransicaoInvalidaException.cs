namespace F2M05.Tdd;

/// <summary>Lançada quando alguém tenta uma transição de status que a máquina de estados não permite.</summary>
public sealed class TransicaoInvalidaException : InvalidOperationException
{
    public TransicaoInvalidaException(StatusPedido de, StatusPedido para)
        : base($"Não é possível mudar o pedido de {de} para {para}.")
    {
        De = de;
        Para = para;
    }

    /// <summary>Status em que o pedido estava.</summary>
    public StatusPedido De { get; }

    /// <summary>Status pedido pela operação recusada.</summary>
    public StatusPedido Para { get; }
}
