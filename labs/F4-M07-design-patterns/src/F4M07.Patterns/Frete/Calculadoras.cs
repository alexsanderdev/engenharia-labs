namespace F4M07.Patterns.Frete;

// Passo 1 — STRATEGY. Uma classe por modalidade, todas cumprindo o mesmo contrato (ICalculadoraDeFrete).
// "Kg iniciado" = Math.Ceiling(peso): 2,3 kg cobra 3 kg; 0 kg cobra 0.

/// <summary>
/// Econômico: grátis com subtotal a partir de R$ 300,00; senão R$ 15,00 + R$ 2,00 por kg iniciado.
/// Prazo: 5 dias úteis para SP, 8 para as demais UFs (compare a UF sem diferenciar maiúsculas).
/// </summary>
public sealed class FreteEconomico : ICalculadoraDeFrete
{
    public string Modalidade => ModalidadeFrete.Economico;

    public CotacaoDeFrete Calcular(PedidoParaFrete pedido) =>
        throw new NotImplementedException("TODO: grátis se Subtotal >= 300; senão 15 + 2 × Math.Ceiling(PesoKg). Prazo 5 (SP) ou 8.");
}

/// <summary>
/// Expresso: R$ 30,00 + R$ 4,00 por kg iniciado, nunca grátis.
/// Prazo: 1 dia útil para SP, 3 para as demais UFs.
/// </summary>
public sealed class FreteExpresso : ICalculadoraDeFrete
{
    public string Modalidade => ModalidadeFrete.Expresso;

    public CotacaoDeFrete Calcular(PedidoParaFrete pedido) =>
        throw new NotImplementedException("TODO: 30 + 4 × Math.Ceiling(PesoKg). Prazo 1 (SP) ou 3.");
}

/// <summary>Retirada na loja: R$ 0,00, pronto em 1 dia útil, qualquer UF.</summary>
public sealed class RetiradaNaLoja : ICalculadoraDeFrete
{
    public string Modalidade => ModalidadeFrete.Retirada;

    public CotacaoDeFrete Calcular(PedidoParaFrete pedido) =>
        throw new NotImplementedException("TODO: new CotacaoDeFrete(Modalidade, 0m, 1).");
}
