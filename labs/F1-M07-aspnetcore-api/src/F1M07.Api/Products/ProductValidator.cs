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
        var errors = new Dictionary<string, string[]>();

        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name))
            errors["Name"] = ["O nome é obrigatório."];
        else if (name.Length > NameMaxLength)
            errors["Name"] = [$"O nome deve ter no máximo {NameMaxLength} caracteres."];

        if (request.Price <= 0)
            errors["Price"] = ["O preço deve ser maior que zero."];

        return errors;
    }
}
