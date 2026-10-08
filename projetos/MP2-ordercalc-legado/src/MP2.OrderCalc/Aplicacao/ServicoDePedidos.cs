using MP2.OrderCalc.Dominio;

namespace MP2.OrderCalc.Aplicacao;

// ---- Portas (seams): o serviço não sabe que existe um Db estático.

public interface ICatalogoDeProdutos
{
    ProdutoCatalogo? Buscar(string sku);
}

public interface ICadastroDeClientes
{
    ClientePedido? Buscar(string id);
}

public interface ICupons
{
    CupomPromocional? Buscar(string codigo);
}

public interface IRegistroDePedidos
{
    /// <summary>Grava o pedido (baixa estoque, consome cupom, promove cliente novo) e devolve o número.</summary>
    string Registrar(PedidoFechado pedido);
}

/// <summary>Pedido: grava e valida bloqueio/estoque/cupom. Orçamento: só calcula (regras herdadas do legado).</summary>
public enum ModoDeCalculo { Pedido, Orcamento }

public sealed record PedidoParaCalcular(
    ClientePedido Cliente,
    IReadOnlyList<ItemPedido> Itens,
    string? Cupom,
    Uf Destino,
    TipoEntrega Entrega);

public sealed record PedidoCalculado(
    Money Subtotal,
    Money Desconto,
    Money Frete,
    Money Imposto,
    Money Total,
    DateTime PrazoEntrega,
    IReadOnlyList<string> Mensagens,
    CupomPromocional? CupomUtilizado);

public sealed record PedidoFechado(
    ClientePedido Cliente,
    IReadOnlyList<ItemPedido> Itens,
    CupomPromocional? CupomUtilizado,
    Money Total,
    DateTime CriadoEm);

public sealed class ServicoDePedidos(
    ICatalogoDeProdutos catalogo,
    ICadastroDeClientes clientes,
    ICupons cupons,
    IRegistroDePedidos registro,
    TimeProvider tempo)
{
    public DateTime Agora() => tempo.GetLocalNow().DateTime;

    public ClientePedido ObterCliente(string? id, ModoDeCalculo modo)
    {
        if (string.IsNullOrEmpty(id)) throw new PedidoInvalidoException("Cliente obrigatório");
        var cliente = clientes.Buscar(id) ?? throw new PedidoInvalidoException("Cliente não encontrado: " + id);
        if (modo == ModoDeCalculo.Pedido && cliente.Bloqueado) throw new PedidoInvalidoException("Cliente bloqueado");
        return cliente;
    }

    public static void ValidarEntrega(Uf destino, TipoEntrega entrega)
    {
        if (entrega == TipoEntrega.Retirada && destino.Sigla != "SP")
            throw new PedidoInvalidoException("Retirada disponível apenas em SP");
    }

    public ItemPedido ResolverItem(string sku, Quantidade quantidade, ModoDeCalculo modo)
    {
        var produto = catalogo.Buscar(sku) ?? throw new PedidoInvalidoException("Produto não encontrado: " + sku);
        if (!produto.Ativo) throw new PedidoInvalidoException("Produto inativo: " + sku);
        if (modo == ModoDeCalculo.Pedido && produto.Estoque < quantidade.Valor)
            throw new PedidoInvalidoException("Estoque insuficiente: " + sku);
        return new ItemPedido(produto, quantidade);
    }

    public PedidoCalculado Calcular(PedidoParaCalcular pedido, ModoDeCalculo modo, DateTime agora)
    {
        var mensagens = new List<string>();

        var subtotal = Money.Zero;
        var descontoItens = Money.Zero;
        var baseImposto = Money.Zero;
        double peso = 0;
        foreach (var item in pedido.Itens)
        {
            subtotal += item.ValorBruto;
            descontoItens += item.DescontoPorVolume;
            if (!item.Produto.IsentoDeImposto) baseImposto += item.ValorLiquido;
            peso += item.PesoKg;
        }

        var baseDesconto = subtotal - descontoItens;
        var descontoCliente = RegrasDeDesconto.DoCliente(pedido.Cliente, baseDesconto, agora, mensagens);
        var cupom = modo == ModoDeCalculo.Pedido ? AvaliarCupom(pedido.Cupom, baseDesconto, agora, mensagens) : null;
        var descontoPedido = RegrasDeDesconto.CombinarComCupom(pedido.Cliente, descontoCliente, cupom, mensagens);
        var desconto = RegrasDeDesconto.AplicarTeto(descontoItens + descontoPedido, subtotal, mensagens);

        var frete = CalcularFrete(pedido, modo, peso, subtotal, desconto, cupom, mensagens);
        var imposto = RegrasDeImposto.Calcular(baseImposto, pedido.Destino);
        var total = (subtotal - desconto + frete + imposto).Arredondar();

        return new PedidoCalculado(
            subtotal, desconto, frete, imposto, total,
            CalendarioDeEntrega.Prazo(agora, pedido.Destino, pedido.Entrega),
            mensagens,
            cupom?.Cupom);
    }

    public string Registrar(PedidoParaCalcular pedido, PedidoCalculado calculado, DateTime criadoEm) =>
        registro.Registrar(new PedidoFechado(pedido.Cliente, pedido.Itens, calculado.CupomUtilizado, calculado.Total, criadoEm));

    private CupomAvaliado? AvaliarCupom(string? codigo, Money baseDesconto, DateTime agora, List<string> mensagens)
    {
        if (string.IsNullOrEmpty(codigo)) return null;
        var normalizado = codigo.Trim().ToUpperInvariant();
        return RegrasDeDesconto.AvaliarCupom(normalizado, cupons.Buscar(normalizado), baseDesconto, agora, mensagens);
    }

    private static Money CalcularFrete(
        PedidoParaCalcular pedido, ModoDeCalculo modo, double peso, Money subtotal, Money desconto,
        CupomAvaliado? cupom, List<string> mensagens)
    {
        if (pedido.Entrega == TipoEntrega.Retirada)
        {
            mensagens.Add("Retirada na loja");
            return Money.Zero;
        }

        var frete = RegrasDeFrete.Calcular(pedido.Destino, peso, pedido.Entrega);

        // Divergência herdada: o orçamento olha o subtotal ANTES do desconto. Caracterizado, não "corrigido".
        var baseFreteGratis = modo == ModoDeCalculo.Pedido ? subtotal - desconto : subtotal;
        if (pedido.Entrega == TipoEntrega.Normal && (baseFreteGratis >= RegrasDeFrete.MinimoFreteGratis || pedido.Cliente.EhVip))
        {
            frete = Money.Zero;
            mensagens.Add("Frete grátis");
        }

        if (cupom is { FreteGratis: true })
        {
            frete = Money.Zero;
            mensagens.Add("Frete grátis (cupom)");
        }

        return frete;
    }
}
