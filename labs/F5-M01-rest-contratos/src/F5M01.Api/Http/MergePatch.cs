using System.Text.Json.Nodes;

namespace F5M01.Api.Http;

/// <summary>
/// JSON Merge Patch (RFC 7396), media type <c>application/merge-patch+json</c>.
/// Regras: membro AUSENTE no patch = não mexe; membro <c>null</c> = remove; objeto = mescla recursivamente;
/// qualquer outro valor (inclusive array) = substitui inteiro.
/// </summary>
public static class MergePatch
{
    public const string MediaType = "application/merge-patch+json";

    /// <summary>
    /// Aplica <paramref name="patch"/> sobre <paramref name="alvo"/> e devolve o RESULTADO, sem alterar nenhum dos dois
    /// (trabalhe sobre cópias: <c>DeepClone()</c>). <c>null</c> representa o JSON <c>null</c>.
    /// </summary>
    public static JsonNode? Aplicar(JsonNode? alvo, JsonNode? patch) =>
        throw new NotImplementedException(
            "TODO (passo 4): implemente o pseudocódigo da seção 2 da RFC 7396 (recursivo). " +
            "Patch que não é objeto substitui tudo; alvo que não é objeto vira {}; valor null remove a chave.");
}
