namespace F6M05.Sagas.Retry;

/// <summary>
/// PRONTO. Falha que tende a desaparecer sozinha (dependência fora do ar, timeout, conflito de
/// concorrência, mensagem que chegou antes da hora). Vale repetir — com limite.
/// </summary>
public class ErroTransitorioException : Exception
{
    public ErroTransitorioException() { }
    public ErroTransitorioException(string message) : base(message) { }
    public ErroTransitorioException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>
/// PRONTO. Falha que NÃO melhora repetindo (mensagem inválida, regra de negócio violada,
/// recurso que não existe). Repetir só gasta recurso e atrasa a fila: vai direto para a DLQ.
/// </summary>
public class ErroPermanenteException : Exception
{
    public ErroPermanenteException() { }
    public ErroPermanenteException(string message) : base(message) { }
    public ErroPermanenteException(string message, Exception inner) : base(message, inner) { }
}
