using F4M07.Patterns.Frete;
using F4M07.Patterns.Legado;
using Microsoft.Extensions.Options;

namespace F4M07.Patterns.Checkout;

/// <summary>Total do pedido discriminado.</summary>
public sealed record TotalDoPedido(decimal Subtotal, decimal TaxaDeServico, CotacaoDeFrete Frete)
{
    public decimal Total => Subtotal + TaxaDeServico + Frete.Valor;
}

/// <summary>
/// Calcula subtotal + taxa de serviço + frete.
/// </summary>
/// <remarks>
/// Passo 7 — ANTI-PADRÃO. Este código veio do legado: alguém já adicionou os parâmetros no construtor, mas o método
/// continua buscando as dependências "por fora", num Singleton estático e num Service Locator. Resultado: a assinatura
/// mente, os testes interferem uns nos outros e só se descobre o erro em runtime.
/// TODO: use o <c>cotador</c> e as <c>opcoes</c> recebidos no construtor e remova o <c>using F4M07.Patterns.Legado</c>.
/// </remarks>
#pragma warning disable CS9113 // parâmetros ainda não usados: é exatamente o que você vai corrigir
public sealed class CalculadoraDeTotalDoPedido(ICotadorDeFrete cotador, IOptions<OpcoesDeCheckout> opcoes)
#pragma warning restore CS9113
{
    /// <summary>Taxa = subtotal × percentual, arredondada para 2 casas (<see cref="MidpointRounding.AwayFromZero"/>).</summary>
    public TotalDoPedido Calcular(decimal subtotal, decimal pesoKg, string uf, string modalidade)
    {
        // ANTI-PADRÃO 1: Singleton estático e mutável.
        var percentual = ConfiguracaoGlobal.Instancia.PercentualDeTaxaDeServico;
        var taxa = Math.Round(subtotal * percentual, 2);

        // ANTI-PADRÃO 2: Service Locator — a dependência não aparece na assinatura.
        var cotadorDoLegado = LocalizadorDeServicos.Resolver<ICotadorDeFrete>();
        var frete = cotadorDoLegado.Cotar(modalidade, new PedidoParaFrete(subtotal, pesoKg, uf));

        return new TotalDoPedido(subtotal, taxa, frete);
    }
}
