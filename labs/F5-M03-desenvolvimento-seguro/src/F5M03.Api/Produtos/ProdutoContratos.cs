namespace F5M03.Api.Produtos;

// TODO (Passo 2 do Lab): crie o contrato de saída do catálogo (ex.: ProdutoResponse(Id, Nome, Preco)),
// sem CustoInterno e sem os bytes da imagem.

/// <summary>ARQUIVO PRONTO (as constantes) — limites do upload de imagem de produto.</summary>
public static class LimitesDeUpload
{
    /// <summary>1 MiB. Acima disso: 413 Payload Too Large.</summary>
    public const long TamanhoMaximoEmBytes = 1 * 1024 * 1024;

    /// <summary>Tipos aceitos (allowlist). Qualquer outro: 415 Unsupported Media Type.</summary>
    public static readonly IReadOnlyList<string> TiposPermitidos = ["image/png", "image/jpeg"];
}
