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
    public static JsonNode? Aplicar(JsonNode? alvo, JsonNode? patch)
    {
        if (patch is not JsonObject patchObjeto) return patch?.DeepClone();

        var resultado = alvo is JsonObject alvoObjeto ? (JsonObject)alvoObjeto.DeepClone() : new JsonObject();
        foreach (var (nome, valor) in patchObjeto)
        {
            if (valor is null)
            {
                resultado.Remove(nome);
                continue;
            }

            resultado.TryGetPropertyValue(nome, out var atual);
            resultado[nome] = Aplicar(atual, valor);
        }

        return resultado;
    }
}
