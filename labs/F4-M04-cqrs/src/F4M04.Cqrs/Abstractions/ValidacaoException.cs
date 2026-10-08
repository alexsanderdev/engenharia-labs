namespace F4M04.Cqrs.Abstractions;

/// <summary>
/// Lançada pelo decorator de validação quando a mensagem (command ou query) é inválida.
/// O handler NÃO chega a ser executado. (No módulo 4.08 você troca isso por Result.)
/// </summary>
public sealed class ValidacaoException : Exception
{
    public ValidacaoException(string nomeDaMensagem, IReadOnlyDictionary<string, string[]> erros)
        : base($"A mensagem {nomeDaMensagem} é inválida: {string.Join("; ", erros.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}"))}")
    {
        NomeDaMensagem = nomeDaMensagem;
        Erros = erros;
    }

    /// <summary>Nome do tipo do command/query validado (ex.: "CriarPedido").</summary>
    public string NomeDaMensagem { get; }

    /// <summary>Erros agrupados por propriedade (ex.: "Itens" → ["..."]).</summary>
    public IReadOnlyDictionary<string, string[]> Erros { get; }
}
