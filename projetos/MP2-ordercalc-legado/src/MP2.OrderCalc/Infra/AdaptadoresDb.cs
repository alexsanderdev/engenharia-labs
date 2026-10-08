using System.Globalization;

using MP2.OrderCalc.Aplicacao;
using MP2.OrderCalc.Dominio;

namespace MP2.OrderCalc.Infra;

// Adaptadores sobre o Db estático (que é infraestrutura e não pode mudar de formato).
// Toda a tradução de strings mágicas ("VIP", "PERCENT", "LIVRO") fica AQUI, na borda.

public sealed class DbCatalogo : ICatalogoDeProdutos
{
    public ProdutoCatalogo? Buscar(string sku) =>
        Db.Produtos.TryGetValue(sku, out var p)
            ? new ProdutoCatalogo(p.Sku, Money.Reais(p.Preco), p.Peso, p.Categoria == "LIVRO", p.Ativo, p.Estoque)
            : null;
}

public sealed class DbClientes : ICadastroDeClientes
{
    public ClientePedido? Buscar(string id) =>
        Db.Clientes.TryGetValue(id, out var c)
            ? new ClientePedido(c.Id, c.Tipo switch { "VIP" => TipoCliente.Vip, "NOVO" => TipoCliente.Novo, _ => TipoCliente.Normal }, c.Bloqueado)
            : null;
}

public sealed class DbCupons : ICupons
{
    public CupomPromocional? Buscar(string codigo) =>
        Db.Cupons.TryGetValue(codigo, out var c)
            ? new CupomPromocional(
                c.Codigo,
                c.Tipo switch { "PERCENT" => TipoCupom.Percentual, "VALOR" => TipoCupom.Valor, "FRETE" => TipoCupom.FreteGratis, _ => TipoCupom.Desconhecido },
                c.Valor,
                c.Validade,
                Money.Reais(c.MinimoPedido),
                c.UsosRestantes)
            : null;
}

public sealed class DbRegistroDePedidos : IRegistroDePedidos
{
    public string Registrar(PedidoFechado pedido)
    {
        var numero = string.Create(CultureInfo.InvariantCulture, $"PED-{pedido.CriadoEm:yyyyMMddHHmmss}-{Db.ProximoId:D4}");
        Db.ProximoId++;

        // Comportamento herdado: SKU repetido em duas linhas baixa o estoque duas vezes (pode ficar negativo).
        foreach (var item in pedido.Itens)
            Db.Produtos[item.Produto.Sku].Estoque -= item.Quantidade.Valor;

        if (pedido.CupomUtilizado is not null)
            Db.Cupons[pedido.CupomUtilizado.Codigo].UsosRestantes--;

        if (pedido.Cliente.Tipo == TipoCliente.Novo)
            Db.Clientes[pedido.Cliente.Id].Tipo = "NORMAL";

        Db.Pedidos.Add(new PedidoGravado { Numero = numero, ClienteId = pedido.Cliente.Id, Total = pedido.Total.Valor, CriadoEm = pedido.CriadoEm });
        return numero;
    }
}
