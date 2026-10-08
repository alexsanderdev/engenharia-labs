using F5M01.Api.Dominio;

namespace F5M01.Api.Http;

/// <summary>
/// ETags e pré-condições HTTP (RFC 9110, seções 8.8.3 e 13). O ETag do pedido é derivado da <see cref="Pedido.Versao"/>:
/// muda sempre que o estado muda e só nessas horas.
/// </summary>
public static class ETags
{
    /// <summary>ETag FORTE do pedido: a versão entre aspas, ex.: <c>"3"</c>. (As aspas fazem parte do valor!)</summary>
    public static string Para(Pedido pedido) =>
        throw new NotImplementedException("TODO (passo 2): devolva a versão do pedido entre aspas duplas, ex.: \"3\".");

    /// <summary>
    /// <c>If-None-Match</c> (GET condicional → 304). Usa comparação FRACA: <c>W/"3"</c> corresponde a <c>"3"</c>.
    /// Aceita lista separada por vírgula e <c>*</c> (corresponde a qualquer representação existente).
    /// Header ausente ou vazio → false.
    /// </summary>
    public static bool IfNoneMatchCorresponde(string? ifNoneMatch, string etagAtual) =>
        throw new NotImplementedException("TODO (passo 2): separe por vírgula, trate '*' e compare ignorando o prefixo W/.");

    /// <summary>
    /// <c>If-Match</c> (concorrência otimista → 412 quando falha). Usa comparação FORTE: ETag fraco NUNCA corresponde.
    /// Header ausente ou vazio → true (não há pré-condição). <c>*</c> → true (o recurso existe).
    /// </summary>
    public static bool IfMatchAtendido(string? ifMatch, string etagAtual) =>
        throw new NotImplementedException("TODO (passo 2): sem header → true; '*' → true; senão algum candidato FORTE (sem W/) igual ao atual.");
}
