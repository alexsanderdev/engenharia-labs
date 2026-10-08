namespace F6M02.ServiceBus.Publicacao;

/// <summary>Mensagem que nunca vai poder ser processada (contrato quebrado): destino certo é a DLQ.</summary>
public sealed class MensagemInvalidaException : Exception
{
    public MensagemInvalidaException() { }
    public MensagemInvalidaException(string message) : base(message) { }
    public MensagemInvalidaException(string message, Exception innerException) : base(message, innerException) { }
}
