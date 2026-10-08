namespace F2M01.CleanCode.Legado;

// Classe utilitária de pedidos. NÃO MEXER - funciona!!!
// Autor: ??? (2019) - alterado por muita gente depois
public static class PedidoUtil
{
    // calcula o pedido
    // f1 = cliente especial, f2 = entrega rapida
    // retorna o total ou -1 se der erro (msg em m)
    public static decimal Calc(List<ItemDoPedido> l, string uf, bool f1, bool f2, out string? m)
    {
        m = null;
        decimal t = 0;
        if (l != null)
        {
            if (l.Count > 0)
            {
                // valida os itens e soma
                for (int i = 0; i < l.Count; i++)
                {
                    var x = l[i];
                    if (x.Quantidade > 0)
                    {
                        if (x.ProdutoAtivo)
                        {
                            t += x.PrecoUnitario * x.Quantidade;
                        }
                        else
                        {
                            m = "Produto inativo: " + x.Sku;
                            return -1;
                        }
                    }
                    else
                    {
                        m = "Quantidade inválida: " + x.Sku;
                        return -1;
                    }
                }

                if (uf != null && uf.Trim().Length == 2)
                {
                    uf = uf.Trim().ToUpper();

                    // desconto de 15% para cliente vip
                    decimal d = 0;
                    if (f1)
                    {
                        d = Math.Round(t * 0.1m, 2, MidpointRounding.AwayFromZero);
                    }
                    else
                    {
                        if (t >= 500)
                        {
                            d = Math.Round(t * 0.05m, 2, MidpointRounding.AwayFromZero);
                        }
                    }

                    decimal t2 = t - d;

                    // frete gratis acima de 200
                    decimal fr = 0;
                    if (t2 < 300 || f2)
                    {
                        if (uf == "SP")
                        {
                            fr = 15;
                        }
                        else if (uf == "RJ" || uf == "MG" || uf == "ES" || uf == "PR" || uf == "SC" || uf == "RS")
                        {
                            fr = 25;
                        }
                        else
                        {
                            fr = 40;
                        }

                        if (f2)
                        {
                            fr = fr * 2; // expresso
                        }
                    }

                    return t2 + fr;
                }
                else
                {
                    m = "UF inválida";
                    return -1;
                }
            }
            else
            {
                m = "Pedido sem itens";
                return -1;
            }
        }
        else
        {
            m = "Pedido sem itens";
            return -1;
        }
    }
}
