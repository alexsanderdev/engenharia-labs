namespace F2M07.Api.Produtos;

public static class ProdutoValidator
{
    public static Dictionary<string, string[]> Validar(ProdutoRequest request)
    {
        var erros = new Dictionary<string, string[]>();
        var sku = request.Sku?.Trim();
        var nome = request.Nome?.Trim();

        if (string.IsNullOrEmpty(sku)) erros["Sku"] = ["O SKU é obrigatório."];
        else if (sku.Length > 30) erros["Sku"] = ["O SKU deve ter no máximo 30 caracteres."];

        if (string.IsNullOrEmpty(nome)) erros["Nome"] = ["O nome é obrigatório."];
        else if (nome.Length > 100) erros["Nome"] = ["O nome deve ter no máximo 100 caracteres."];

        if (request.Preco <= 0) erros["Preco"] = ["O preço deve ser maior que zero."];

        return erros;
    }
}
