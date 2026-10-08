namespace F4M02.Pedidos.Domain;

/// <summary>
/// Catálogo das regras do contexto de Pedidos, com códigos estáveis. É a linguagem ubíqua
/// em forma de constante: o mesmo nome aparece na conversa com o negócio, no código, nos testes e
/// nas mensagens de erro da API.
/// </summary>
/// <remarks>PRONTO — use estas constantes ao lançar <see cref="Comum.RegraDeNegocioVioladaException"/>.</remarks>
public static class Regras
{
    // Value objects
    public const string DinheiroNegativo = "dinheiro.negativo";
    public const string MoedaInvalida = "dinheiro.moeda-invalida";
    public const string MoedasDiferentes = "dinheiro.moedas-diferentes";
    public const string QuantidadeInvalida = "quantidade.invalida";
    public const string SkuInvalido = "sku.invalido";
    public const string EnderecoInvalido = "endereco.invalido";

    // Agregado Pedido
    public const string ProdutoInativo = "pedido.produto-inativo";
    public const string ProdutoRepetido = "pedido.produto-repetido";
    public const string ItemNaoEncontrado = "pedido.item-nao-encontrado";
    public const string LimiteDeItens = "pedido.limite-de-itens";
    public const string PedidoNaoEditavel = "pedido.nao-editavel";
    public const string PedidoSemItens = "pedido.sem-itens";
    public const string TransicaoInvalida = "pedido.transicao-invalida";
    public const string MotivoObrigatorio = "pedido.motivo-obrigatorio";
    public const string DescontoInvalido = "pedido.desconto-invalido";

    // Política de desconto (domain service)
    public const string ClienteDiferente = "desconto.cliente-diferente";
}
