namespace F2M01.CleanCode;

/// <summary>
/// Orquestra o cálculo: valida, soma, aplica desconto e frete. Cada passo é um método pequeno
/// com nome de negócio; quem lê <see cref="Calcular"/> entende o fluxo sem ler os detalhes.
/// </summary>
public sealed class CalculadoraDePedido(ValidadorDePedido validador, RegrasDeFrete regrasDeFrete)
{
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
        _ = validador;
        _ = regrasDeFrete;
        // Meta: o corpo deste método deve ler como uma frase — garantir válido, somar, descontar, calcular frete.
        throw new NotImplementedException("TODO: use o validador e as regras de frete; extraia CalcularDesconto com constantes nomeadas");
    }
}
