using F5M01.Api.Dominio;

namespace F5M01.Api.Http;

/// <summary>
/// Fábrica de respostas de erro (PRONTA): todo erro sai como ProblemDetails (RFC 9457, <c>application/problem+json</c>)
/// com a extensão <c>code</c> (código estável, para máquinas). O <c>traceId</c> é acrescentado pelo
/// <c>CustomizeProblemDetails</c> do Program.cs. O assunto ProblemDetails é aprofundado no módulo 5.02.
/// </summary>
public static class Problemas
{
    public const string CodigoValidacao = "requisicao.validacao";
    public const string CodigoNaoEncontrado = "pedido.nao_encontrado";
    public const string CodigoVersaoDesatualizada = "pedido.versao_desatualizada";
    public const string CodigoMediaTypeNaoSuportado = "requisicao.media_type_nao_suportado";
    public const string CodigoJsonInvalido = "requisicao.json_invalido";

    public static IResult Criar(int status, string titulo, string code, string detalhe) =>
        TypedResults.Problem(
            title: titulo,
            detail: detalhe,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    /// <summary>400 (ou outro status, ex.: 422 para PATCH) com <c>errors</c> por campo.</summary>
    public static IResult Validacao(IDictionary<string, string[]> erros, int status = StatusCodes.Status400BadRequest) =>
        TypedResults.Problem(new HttpValidationProblemDetails(erros)
        {
            Status = status,
            Title = "Requisição inválida",
            Detail = "Um ou mais campos são inválidos.",
            Extensions = { ["code"] = CodigoValidacao },
        });

    public static IResult PedidoNaoEncontrado(Guid id) =>
        Criar(StatusCodes.Status404NotFound, "Pedido não encontrado", CodigoNaoEncontrado, $"Não existe pedido com id {id}.");

    public static IResult VersaoDesatualizada() =>
        Criar(StatusCodes.Status412PreconditionFailed, "Versão desatualizada", CodigoVersaoDesatualizada,
            "O pedido foi alterado por outra requisição. Faça GET de novo, reaplique a mudança e envie com o novo ETag em If-Match.");

    public static IResult MediaTypeNaoSuportado(string esperado) =>
        Criar(StatusCodes.Status415UnsupportedMediaType, "Media type não suportado", CodigoMediaTypeNaoSuportado,
            $"Envie o corpo com Content-Type: {esperado}.");

    public static IResult JsonInvalido() =>
        Criar(StatusCodes.Status400BadRequest, "JSON inválido", CodigoJsonInvalido, "O corpo da requisição não é um JSON válido.");

    /// <summary>Erro de domínio → 409 (conflito com o estado) ou 422 (regra de negócio).</summary>
    public static IResult De(ErroDominio erro)
    {
        ArgumentNullException.ThrowIfNull(erro);
        return erro.Tipo switch
        {
            TipoErro.Conflito => Criar(StatusCodes.Status409Conflict, "Conflito com o estado do pedido", erro.Code, erro.Mensagem),
            _ => Criar(StatusCodes.Status422UnprocessableEntity, "Regra de negócio violada", erro.Code, erro.Mensagem),
        };
    }
}
