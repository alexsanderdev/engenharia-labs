namespace F5M03.Api.Produtos;

/// <summary>Saída pública do catálogo: sem CustoInterno.</summary>
public sealed record ProdutoResponse(Guid Id, string Nome, decimal Preco);

/// <summary>ARQUIVO PRONTO (as constantes) — limites do upload de imagem de produto.</summary>
public static class LimitesDeUpload
{
    /// <summary>1 MiB. Acima disso: 413 Payload Too Large.</summary>
    public const long TamanhoMaximoEmBytes = 1 * 1024 * 1024;

    /// <summary>Tipos aceitos (allowlist). Qualquer outro: 415 Unsupported Media Type.</summary>
    public static readonly IReadOnlyList<string> TiposPermitidos = ["image/png", "image/jpeg"];
}
