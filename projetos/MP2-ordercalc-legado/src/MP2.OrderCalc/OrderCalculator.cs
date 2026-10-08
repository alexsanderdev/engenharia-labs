using MP2.OrderCalc.Aplicacao;
using MP2.OrderCalc.Dominio;
using MP2.OrderCalc.Infra;

namespace MP2.OrderCalc;

/// <summary>
/// Fachada com a MESMA assinatura do legado (loja web, televendas e relatório continuam chamando isto).
/// Ela só traduz primitivos ⇄ domínio, na ordem de validação original, e delega ao <see cref="ServicoDePedidos"/>.
/// </summary>
public class OrderCalculator
{
    private readonly ServicoDePedidos _servico;

    public OrderCalculator() : this(TimeProvider.System) { }

    /// <summary>Seam de tempo: em teste, passe um FakeTimeProvider.</summary>
    public OrderCalculator(TimeProvider tempo)
        : this(new ServicoDePedidos(new DbCatalogo(), new DbClientes(), new DbCupons(), new DbRegistroDePedidos(), tempo)) { }

    public OrderCalculator(ServicoDePedidos servico) => _servico = servico;

    /// <param name="itens">Formato legado: "SKU:QTD;SKU:QTD".</param>
    /// <param name="tipoFrete">"NORMAL", "EXPRESSO" ou "RETIRADA".</param>
    public ResultadoPedido Calcular(string clienteId, string itens, string cupom, string uf, string tipoFrete)
    {
        // TODO(pós-MP2): trocar Console por ILogger. Mantido para não mudar a saída que o suporte monitora.
        Console.WriteLine("[OrderCalc] Calculando pedido para " + clienteId);
        var r = Executar(ModoDeCalculo.Pedido, clienteId, itens, cupom, uf, tipoFrete);
        if (r.Status == "OK") Console.WriteLine("[OrderCalc] Pedido " + r.Numero + " gravado. Total: " + r.Total);
        return r;
    }

    public ResultadoPedido CalcularOrcamento(string clienteId, string itens, string uf, string tipoFrete) =>
        Executar(ModoDeCalculo.Orcamento, clienteId, itens, cupom: null, uf, tipoFrete);

    private ResultadoPedido Executar(ModoDeCalculo modo, string? clienteId, string? itens, string? cupom, string? uf, string? tipoFrete)
    {
        var agora = _servico.Agora();
        var r = new ResultadoPedido { CriadoEm = agora };
        try
        {
            var cliente = _servico.ObterCliente(clienteId, modo);
            var destino = Uf.Ler(uf);
            var entrega = LerTipoEntrega(tipoFrete);
            ServicoDePedidos.ValidarEntrega(destino, entrega);

            var lista = new List<ItemPedido>();
            foreach (var (sku, quantidade) in LerItens(itens))
            {
                var item = _servico.ResolverItem(sku, quantidade, modo);
                lista.Add(item);
                r.Linhas.Add(item.Descricao); // o legado devolve as linhas já lidas mesmo quando um item seguinte falha
            }

            if (lista.Count == 0) throw new PedidoInvalidoException("Pedido sem itens");

            var pedido = new PedidoParaCalcular(cliente, lista, cupom, destino, entrega);
            var calculado = _servico.Calcular(pedido, modo, agora);

            r.Subtotal = calculado.Subtotal.Valor;
            r.Desconto = calculado.Desconto.Valor;
            r.Frete = calculado.Frete.Valor;
            r.Imposto = calculado.Imposto.Valor;
            r.Total = calculado.Total.Valor;
            r.PrazoEntrega = calculado.PrazoEntrega;
            r.Mensagens.AddRange(calculado.Mensagens);

            if (modo == ModoDeCalculo.Pedido)
            {
                r.Numero = _servico.Registrar(pedido, calculado, agora);
                r.Status = "OK";
            }
            else
            {
                r.Status = "ORCAMENTO";
            }
        }
        catch (PedidoInvalidoException ex)
        {
            r.Status = "ERRO";
            r.Erro = ex.Message;
        }

        return r;
    }

    private static TipoEntrega LerTipoEntrega(string? texto) => texto switch
    {
        "NORMAL" => TipoEntrega.Normal,
        "EXPRESSO" => TipoEntrega.Expresso,
        "RETIRADA" => TipoEntrega.Retirada,
        _ => throw new PedidoInvalidoException("Tipo de frete inválido: " + texto),
    };

    /// <summary>
    /// Lê "SKU:QTD;SKU:QTD" de forma preguiçosa (yield): cada item é lido e validado contra o catálogo
    /// antes de o próximo ser lido. É isso que preserva a ordem das mensagens de erro do legado
    /// (um produto inexistente no item 1 vence uma quantidade inválida no item 2).
    /// </summary>
    private static IEnumerable<(string Sku, Quantidade Quantidade)> LerItens(string? itens)
    {
        if (string.IsNullOrWhiteSpace(itens)) throw new PedidoInvalidoException("Pedido sem itens");

        foreach (var trecho in itens.Split(';').Select(p => p.Trim()).Where(p => p.Length > 0))
        {
            var partes = trecho.Split(':');
            if (partes.Length != 2) throw new PedidoInvalidoException("Item inválido: " + trecho);
            var sku = partes[0].Trim().ToUpperInvariant();
            if (!int.TryParse(partes[1], out var numero)) throw new PedidoInvalidoException("Quantidade inválida: " + trecho);
            if (!Quantidade.TentarCriar(numero, out var quantidade))
                throw new PedidoInvalidoException("Quantidade deve ser maior que zero: " + sku);
            yield return (sku, quantidade);
        }
    }
}
