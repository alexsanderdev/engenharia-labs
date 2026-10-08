namespace F2M01.CleanCode;

/// <summary>
/// Orquestra o cálculo: valida, soma, aplica desconto e frete. Cada passo é um método pequeno
/// com nome de negócio; quem lê <see cref="Calcular"/> entende o fluxo sem ler os detalhes.
/// </summary>
public sealed class CalculadoraDePedido(ValidadorDePedido validador, RegrasDeFrete regrasDeFrete)
{
    public const decimal PercentualDeDescontoVip = 0.10m;
    public const decimal PercentualDeDescontoPorVolume = 0.05m;
    public const decimal ValorMinimoParaDescontoPorVolume = 500m;

    public CalculadoraDePedido() : this(new ValidadorDePedido(), new RegrasDeFrete())
    {
    }

    /// <summary>
    /// Regras (as mesmas do legado):
    /// <list type="bullet">
    /// <item>pedido inválido → <see cref="PedidoInvalidoException"/> com todos os erros do validador;</item>
    /// <item>VIP ganha 10%; cliente comum ganha 5% se o subtotal for ≥ 500; descontos NÃO acumulam;</item>
    /// <item>desconto arredondado para 2 casas (<see cref="MidpointRounding.AwayFromZero"/>);</item>
    /// <item>frete calculado pelas <see cref="RegrasDeFrete"/> sobre o valor já com desconto.</item>
    /// </list>
    /// </summary>
    public ResumoDoPedido Calcular(PedidoParaCalculo pedido)
    {
        GarantirQueEhValido(pedido);

        var subtotal = pedido.Itens.Sum(item => item.Subtotal);
        var desconto = CalcularDesconto(subtotal, pedido.Cliente);
        var frete = regrasDeFrete.Calcular(pedido.Uf, subtotal - desconto, pedido.Entrega);

        return new ResumoDoPedido(subtotal, desconto, frete);
    }

    private void GarantirQueEhValido(PedidoParaCalculo pedido)
    {
        var resultado = validador.Validar(pedido);
        if (!resultado.EhValido)
        {
            throw new PedidoInvalidoException(resultado.Erros);
        }
    }

    private static decimal CalcularDesconto(decimal subtotal, TipoDeCliente cliente)
    {
        var percentual = PercentualDeDesconto(subtotal, cliente);
        return Math.Round(subtotal * percentual, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal PercentualDeDesconto(decimal subtotal, TipoDeCliente cliente) => cliente switch
    {
        TipoDeCliente.Vip => PercentualDeDescontoVip,
        _ when subtotal >= ValorMinimoParaDescontoPorVolume => PercentualDeDescontoPorVolume,
        _ => 0m,
    };
}
