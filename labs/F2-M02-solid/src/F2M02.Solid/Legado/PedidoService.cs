using System.Globalization;
using System.Text.Json;

using F2M02.Solid.Dominio;
using F2M02.Solid.Infraestrutura;

namespace F2M02.Solid.Legado;

/// <summary>
/// O "serviço de pedido" que todo mundo já viu: valida, carrega produto, calcula desconto,
/// grava no banco, manda e-mail e publica no Kafka — tudo no mesmo método, dependendo de
/// classes CONCRETAS de infraestrutura. Cada um desses é um motivo diferente para mudar.
/// </summary>
public class PedidoService(BancoDeDadosSql banco, ServidorSmtp smtp, ProdutorKafka kafka)
{
    public Pedido CriarPedido(Guid clienteId, string email, List<(Guid ProdutoId, int Quantidade)> itens, string? cupom)
    {
        // 1. validação
        if (itens == null || itens.Count == 0)
        {
            throw new PedidoInvalidoException("Pedido precisa de ao menos um item");
        }

        foreach (var i in itens)
        {
            if (i.Quantidade <= 0)
            {
                throw new PedidoInvalidoException("Quantidade deve ser maior que zero");
            }
        }

        // 2. carrega produtos (SQL direto)
        var linhas = new List<ItemPedido>();
        foreach (var i in itens)
        {
            var p = banco.BuscarProduto(i.ProdutoId) ?? throw new PedidoInvalidoException($"Produto não encontrado: {i.ProdutoId}");
            if (!p.Ativo)
            {
                throw new PedidoInvalidoException($"Produto inativo: {p.Nome}");
            }

            linhas.Add(new ItemPedido(p.Id, p.Nome, p.Preco, i.Quantidade));
        }

        // 3. desconto — todo cupom novo é mais um case aqui (e mais um deploy deste serviço)
        var subtotal = linhas.Sum(l => l.Subtotal);
        decimal desconto;
        switch (cupom?.Trim().ToUpperInvariant())
        {
            case null:
            case "":
                desconto = 0;
                break;
            case "BLACKFRIDAY":
                desconto = Math.Round(subtotal * 0.20m, 2, MidpointRounding.AwayFromZero);
                break;
            case "PRIMEIRACOMPRA":
                desconto = Math.Min(Math.Round(subtotal * 0.10m, 2, MidpointRounding.AwayFromZero), 50m);
                break;
            case "BEMVINDO30":
                desconto = Math.Min(30m, subtotal);
                break;
            default:
                throw new PedidoInvalidoException($"Cupom inválido: {cupom}");
        }

        // 4. grava
        var pedido = new Pedido(Guid.NewGuid(), clienteId, linhas, desconto);
        banco.InserirPedido(pedido);

        // 5. e-mail (se o SMTP cair, o pedido já foi gravado... e o evento não sai)
        smtp.Enviar(email, "Pedido recebido",
            $"Olá! Seu pedido {pedido.Id} foi criado. Total: R$ {pedido.Total.ToString("F2", CultureInfo.InvariantCulture)}");

        // 6. evento
        kafka.Produzir("pedidos.criados", pedido.Id.ToString(),
            JsonSerializer.Serialize(new { pedido.Id, pedido.ClienteId, pedido.Total }));

        return pedido;
    }
}
