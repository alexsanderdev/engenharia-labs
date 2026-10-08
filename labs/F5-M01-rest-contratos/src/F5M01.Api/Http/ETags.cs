using F5M01.Api.Dominio;

namespace F5M01.Api.Http;

/// <summary>
/// ETags e pré-condições HTTP (RFC 9110, seções 8.8.3 e 13). O ETag do pedido é derivado da <see cref="Pedido.Versao"/>:
/// muda sempre que o estado muda e só nessas horas.
/// </summary>
public static class ETags
{
    /// <summary>ETag FORTE do pedido: a versão entre aspas, ex.: <c>"3"</c>. (As aspas fazem parte do valor!)</summary>
    public static string Para(Pedido pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        return $"\"{pedido.Versao}\"";
    }

    /// <summary>
    /// <c>If-None-Match</c> (GET condicional → 304). Usa comparação FRACA: <c>W/"3"</c> corresponde a <c>"3"</c>.
    /// Aceita lista separada por vírgula e <c>*</c> (corresponde a qualquer representação existente).
    /// Header ausente ou vazio → false.
    /// </summary>
    public static bool IfNoneMatchCorresponde(string? ifNoneMatch, string etagAtual)
    {
        if (string.IsNullOrWhiteSpace(ifNoneMatch)) return false;
        var atual = SemPrefixoFraco(etagAtual);
        foreach (var candidato in Separar(ifNoneMatch))
        {
            if (candidato == "*") return true;
            if (SemPrefixoFraco(candidato) == atual) return true;
        }
        return false;
    }

    /// <summary>
    /// <c>If-Match</c> (concorrência otimista → 412 quando falha). Usa comparação FORTE: ETag fraco NUNCA corresponde.
    /// Header ausente ou vazio → true (não há pré-condição). <c>*</c> → true (o recurso existe).
    /// </summary>
    public static bool IfMatchAtendido(string? ifMatch, string etagAtual)
    {
        if (string.IsNullOrWhiteSpace(ifMatch)) return true;
        foreach (var candidato in Separar(ifMatch))
        {
            if (candidato == "*") return true;
            if (!candidato.StartsWith("W/", StringComparison.Ordinal) && candidato == etagAtual) return true;
        }
        return false;
    }

    private static string[] Separar(string header) =>
        header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string SemPrefixoFraco(string etag) =>
        etag.StartsWith("W/", StringComparison.Ordinal) ? etag[2..] : etag;
}
