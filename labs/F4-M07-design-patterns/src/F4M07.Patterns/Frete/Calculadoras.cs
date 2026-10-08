namespace F4M07.Patterns.Frete;

/// <summary>
/// Econômico: grátis com subtotal a partir de R$ 300,00; senão R$ 15,00 + R$ 2,00 por kg iniciado.
/// Prazo: 5 dias úteis para SP, 8 para as demais UFs.
/// </summary>
public sealed class FreteEconomico : ICalculadoraDeFrete
{
    public string Modalidade => ModalidadeFrete.Economico;

    public CotacaoDeFrete Calcular(PedidoParaFrete pedido)
    {
        var valor = pedido.Subtotal >= 300m ? 0m : 15m + 2m * KgIniciados(pedido.PesoKg);
        var prazo = EhSaoPaulo(pedido.Uf) ? 5 : 8;
        return new CotacaoDeFrete(Modalidade, valor, prazo);
    }

    internal static decimal KgIniciados(decimal pesoKg) => Math.Ceiling(Math.Max(pesoKg, 0m));

    internal static bool EhSaoPaulo(string uf) => string.Equals(uf, "SP", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Expresso: R$ 30,00 + R$ 4,00 por kg iniciado, nunca grátis.
/// Prazo: 1 dia útil para SP, 3 para as demais UFs.
/// </summary>
public sealed class FreteExpresso : ICalculadoraDeFrete
{
    public string Modalidade => ModalidadeFrete.Expresso;

    public CotacaoDeFrete Calcular(PedidoParaFrete pedido)
    {
        var valor = 30m + 4m * FreteEconomico.KgIniciados(pedido.PesoKg);
        var prazo = FreteEconomico.EhSaoPaulo(pedido.Uf) ? 1 : 3;
        return new CotacaoDeFrete(Modalidade, valor, prazo);
    }
}

/// <summary>Retirada na loja: R$ 0,00, pronto em 1 dia útil, qualquer UF.</summary>
public sealed class RetiradaNaLoja : ICalculadoraDeFrete
{
    public string Modalidade => ModalidadeFrete.Retirada;

    public CotacaoDeFrete Calcular(PedidoParaFrete pedido) => new(Modalidade, 0m, 1);
}
