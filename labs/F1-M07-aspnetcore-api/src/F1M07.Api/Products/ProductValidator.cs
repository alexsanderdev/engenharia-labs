namespace F1M07.Api.Products;

/// <summary>Regras de validação de entrada de produto.</summary>
public static class ProductValidator
{
    public const int NameMaxLength = 100;

    /// <summary>
    /// Valida o request e devolve um dicionário no formato do ValidationProblem
    /// (chave = nome do campo, valor = mensagens). Vazio = válido.
    /// Regras:
    /// - "Name": obrigatório (não nulo/branco) e com no máximo <see cref="NameMaxLength"/> caracteres (após Trim).
    /// - "Price": maior que zero.
    /// Um campo com problema aparece UMA vez no dicionário, com uma ou mais mensagens.
    /// </summary>
    public static Dictionary<string, string[]> Validate(ProductRequest request)
    {
        throw new NotImplementedException(
            "TODO: valide Name (obrigatório, máx. 100 após Trim) e Price (> 0) e devolva os erros por campo.");
    }
}
