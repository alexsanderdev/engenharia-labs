namespace F2M07.Api.Pedidos;

/// <summary>Validação de formato da entrada (as regras que dependem do banco ficam no handler).</summary>
public static class PedidoValidator
{
    public static Dictionary<string, string[]> Validar(CriarPedidoRequest request)
    {
        var erros = new Dictionary<string, string[]>();

        if (request.Itens is null || request.Itens.Count == 0)
        {
            erros["Itens"] = ["O pedido precisa de ao menos um item."];
            return erros;
        }

        for (var i = 0; i < request.Itens.Count; i++)
        {
            if (request.Itens[i].Quantidade <= 0)
                erros[$"Itens[{i}].Quantidade"] = ["A quantidade deve ser maior que zero."];
        }

        return erros;
    }
}
