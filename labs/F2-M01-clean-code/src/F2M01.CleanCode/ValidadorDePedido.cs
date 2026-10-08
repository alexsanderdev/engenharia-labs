namespace F2M01.CleanCode;

/// <summary>
/// Regras de validação do pedido, extraídas do método gigante do legado.
/// Diferente do legado (que parava no primeiro erro), devolve TODOS os erros, na ordem:
/// itens (um erro por item: quantidade inválida tem prioridade sobre produto inativo) e depois a UF.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822", Justification = "Instância de propósito: a classe é injetada e pode ganhar dependências depois.")]
public sealed class ValidadorDePedido
{
    private const int TamanhoDaUf = 2;

    /// <summary>
    /// Mensagens (iguais às do legado):
    /// "Pedido sem itens" · "Quantidade inválida: {sku}" · "Produto inativo: {sku}" · "UF inválida".
    /// Pedido sem itens devolve só esse erro.
    /// </summary>
    public ResultadoDaValidacao Validar(PedidoParaCalculo pedido)
    {
        ArgumentNullException.ThrowIfNull(pedido);

        if (pedido.Itens is not { Count: > 0 })
        {
            return new ResultadoDaValidacao(["Pedido sem itens"]);
        }

        List<string> erros = [.. pedido.Itens.Select(ValidarItem).OfType<string>()];

        if (!UfEhValida(pedido.Uf))
        {
            erros.Add("UF inválida");
        }

        return new ResultadoDaValidacao(erros);
    }

    private static string? ValidarItem(ItemDoPedido item) => item switch
    {
        { Quantidade: <= 0 } => $"Quantidade inválida: {item.Sku}",
        { ProdutoAtivo: false } => $"Produto inativo: {item.Sku}",
        _ => null,
    };

    private static bool UfEhValida(string? uf) => uf?.Trim().Length == TamanhoDaUf;
}
