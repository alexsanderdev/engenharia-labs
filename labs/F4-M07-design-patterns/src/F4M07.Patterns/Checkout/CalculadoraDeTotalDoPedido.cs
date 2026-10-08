using F4M07.Patterns.Frete;
using Microsoft.Extensions.Options;

namespace F4M07.Patterns.Checkout;

/// <summary>Total do pedido discriminado.</summary>
public sealed record TotalDoPedido(decimal Subtotal, decimal TaxaDeServico, CotacaoDeFrete Frete)
{
    public decimal Total => Subtotal + TaxaDeServico + Frete.Valor;
}

/// <summary>
/// Calcula subtotal + taxa de serviço + frete. Dependências EXPLÍCITAS no construtor: dá para ler a assinatura e saber
/// do que a classe precisa, e o teste monta duas instâncias com configurações diferentes lado a lado.
/// </summary>
public sealed class CalculadoraDeTotalDoPedido(ICotadorDeFrete cotador, IOptions<OpcoesDeCheckout> opcoes)
{
    /// <summary>Taxa = subtotal × percentual, arredondada para 2 casas (<see cref="MidpointRounding.AwayFromZero"/>).</summary>
    public TotalDoPedido Calcular(decimal subtotal, decimal pesoKg, string uf, string modalidade)
    {
        var taxa = Math.Round(subtotal * opcoes.Value.PercentualDeTaxaDeServico, 2, MidpointRounding.AwayFromZero);
        var frete = cotador.Cotar(modalidade, new PedidoParaFrete(subtotal, pesoKg, uf));
        return new TotalDoPedido(subtotal, taxa, frete);
    }
}
