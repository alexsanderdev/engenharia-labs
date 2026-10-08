namespace F2M01.CleanCode.Legado;

// Classe utilitária de pedidos. NÃO MEXER - funciona!!!
// (Comentário original. Depois da refatoração, isto virou uma FACHADA que só traduz
// a assinatura antiga para a API nova — existe para não quebrar quem ainda chama Calc.)
public static class PedidoUtil
{
    /// <summary>
    /// Assinatura legada preservada: <paramref name="f1"/> = cliente VIP, <paramref name="f2"/> = entrega expressa.
    /// Devolve o total ou -1 com a primeira mensagem de erro em <paramref name="m"/>.
    /// </summary>
    public static decimal Calc(List<ItemDoPedido> l, string uf, bool f1, bool f2, out string? m)
    {
        var pedido = new PedidoParaCalculo(
            l ?? [],
            uf,
            f1 ? TipoDeCliente.Vip : TipoDeCliente.Comum,
            f2 ? ModalidadeDeEntrega.Expressa : ModalidadeDeEntrega.Padrao);

        try
        {
            m = null;
            return new CalculadoraDePedido().Calcular(pedido).Total;
        }
        catch (PedidoInvalidoException ex)
        {
            m = ex.Erros[0];
            return -1;
        }
    }
}
