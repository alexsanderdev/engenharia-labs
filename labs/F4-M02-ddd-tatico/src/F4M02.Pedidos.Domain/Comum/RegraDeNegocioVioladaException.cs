namespace F4M02.Pedidos.Domain.Comum;

/// <summary>
/// Lançada quando uma operação violaria uma regra do domínio (invariante).
/// <see cref="Regra"/> traz um código estável, da lista em <see cref="Regras"/>, que a camada de aplicação
/// pode traduzir para HTTP 422/ProblemDetails (ou para um Result, no módulo de Result Pattern).
/// </summary>
/// <remarks>PRONTO — leia, não precisa alterar.</remarks>
public sealed class RegraDeNegocioVioladaException : Exception
{
    public RegraDeNegocioVioladaException(string regra, string mensagem)
        : base(mensagem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regra);
        Regra = regra;
    }

    /// <summary>Código estável da regra violada (ex.: <c>pedido.sem-itens</c>).</summary>
    public string Regra { get; }
}
