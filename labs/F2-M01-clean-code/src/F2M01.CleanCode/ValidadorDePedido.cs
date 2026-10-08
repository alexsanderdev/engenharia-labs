namespace F2M01.CleanCode;

/// <summary>
/// Regras de validação do pedido, extraídas do método gigante do legado.
/// Diferente do legado (que parava no primeiro erro), devolve TODOS os erros, na ordem:
/// itens (um erro por item: quantidade inválida tem prioridade sobre produto inativo) e depois a UF.
/// </summary>
public sealed class ValidadorDePedido
{
    /// <summary>
    /// Mensagens (iguais às do legado):
    /// "Pedido sem itens" · "Quantidade inválida: {sku}" · "Produto inativo: {sku}" · "UF inválida".
    /// Pedido sem itens devolve só esse erro.
    /// </summary>
    public ResultadoDaValidacao Validar(PedidoParaCalculo pedido)
    {
        // Dica: um método privado pequeno por regra (ValidarItem, UfEhValida) e uma constante TamanhoDaUf.
        throw new NotImplementedException("TODO: extraia a validação de PedidoUtil.Calc e devolva TODOS os erros, na ordem");
    }
}
