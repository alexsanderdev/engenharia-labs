namespace F4M08.Api.Resultados;

/// <summary>
/// Categoria do erro. Diz O QUE deu errado em termos de negócio, não qual status HTTP usar:
/// a tradução para HTTP fica na borda (<c>ErrorHttpMapping</c>), não no domínio.
/// </summary>
public enum ErrorType
{
    /// <summary>Regra de negócio violada por uma requisição bem formada (→ 422).</summary>
    Failure = 0,

    /// <summary>Entrada inválida: campo obrigatório, formato, faixa (→ 400).</summary>
    Validation = 1,

    /// <summary>O recurso alvo não existe (→ 404).</summary>
    NotFound = 2,

    /// <summary>A operação conflita com o estado atual do recurso (→ 409).</summary>
    Conflict = 3,

    /// <summary>Quem pediu não tem permissão sobre este recurso (→ 403).</summary>
    Forbidden = 4,
}

/// <summary>
/// Erro de negócio tipado e imutável. <see cref="Code"/> é um identificador ESTÁVEL
/// ("pedido.nao_encontrado"): clientes da API e alertas podem depender dele, então
/// trate a lista de códigos como contrato público (mudar código = breaking change).
/// <see cref="Message"/> é para humanos e pode mudar à vontade.
/// </summary>
#pragma warning disable CA1716 // "Error" é o nome consagrado do padrão (ErrorOr, FluentResults...); o lab é só C#
public sealed record Error(string Code, string Message, ErrorType Type)
#pragma warning restore CA1716
{
    /// <summary>"Nenhum erro": usado internamente pelos resultados de sucesso.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Erros por campo (só para <see cref="ErrorType.Validation"/>). Chave = nome do campo no JSON.</summary>
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; init; }

    /// <summary>Erro de validação, opcionalmente com erros por campo.</summary>
    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]>? errors = null) =>
        Criar(code, message, ErrorType.Validation) with { ValidationErrors = errors };

    public static Error NotFound(string code, string message) => Criar(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => Criar(code, message, ErrorType.Conflict);

    public static Error Forbidden(string code, string message) => Criar(code, message, ErrorType.Forbidden);

    public static Error Failure(string code, string message) => Criar(code, message, ErrorType.Failure);

    /// <summary>Todas as fábricas exigem código e mensagem preenchidos (<see cref="ArgumentException"/> se vazios).</summary>
    private static Error Criar(string code, string message, ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new Error(code, message, type);
    }
}
