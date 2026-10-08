namespace F2M01.CleanCode;

/// <summary>Uma linha do pedido: qual produto (SKU), por quanto e quantas unidades.</summary>
public sealed record ItemDoPedido(string Sku, decimal PrecoUnitario, int Quantidade, bool ProdutoAtivo = true)
{
    public decimal Subtotal => PrecoUnitario * Quantidade;
}

/// <summary>Substitui o <c>bool f1</c> do legado: o tipo diz o que o valor significa.</summary>
public enum TipoDeCliente
{
    Comum,
    Vip,
}

/// <summary>Substitui o <c>bool f2</c> do legado.</summary>
public enum ModalidadeDeEntrega
{
    Padrao,
    Expressa,
}

/// <summary>Tudo o que a calculadora precisa saber sobre o pedido, num único parâmetro com nome.</summary>
public sealed record PedidoParaCalculo(
    IReadOnlyList<ItemDoPedido> Itens,
    string Uf,
    TipoDeCliente Cliente = TipoDeCliente.Comum,
    ModalidadeDeEntrega Entrega = ModalidadeDeEntrega.Padrao);

/// <summary>Resultado detalhado do cálculo (o legado só devolvia o total).</summary>
public sealed record ResumoDoPedido(decimal Subtotal, decimal Desconto, decimal Frete)
{
    public decimal Total => Subtotal - Desconto + Frete;
}

/// <summary>Resultado da validação: todos os problemas encontrados, na ordem em que aparecem.</summary>
public sealed record ResultadoDaValidacao(IReadOnlyList<string> Erros)
{
    public bool EhValido => Erros.Count == 0;
}

/// <summary>Lançada pela <see cref="CalculadoraDePedido"/> quando o pedido não passa na validação.</summary>
public sealed class PedidoInvalidoException(IReadOnlyList<string> erros)
    : Exception("Pedido inválido: " + string.Join("; ", erros))
{
    public IReadOnlyList<string> Erros { get; } = erros;
}
