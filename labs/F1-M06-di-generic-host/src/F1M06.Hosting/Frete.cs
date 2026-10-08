using Microsoft.Extensions.DependencyInjection;

namespace F1M06.Hosting;

/// <summary>Chaves dos keyed services de frete.</summary>
public static class ChavesFrete
{
    public const string Padrao = "padrao";
    public const string Expresso = "expresso";
}

/// <summary>Calcula o frete de um pedido.</summary>
public interface ICalculadoraDeFrete
{
    decimal Calcular(decimal valorPedido);
}

/// <summary>R$ 20,00; grátis a partir de R$ 200,00.</summary>
public sealed class FretePadrao : ICalculadoraDeFrete
{
    public decimal Calcular(decimal valorPedido) => valorPedido >= 200m ? 0m : 20m;
}

/// <summary>R$ 45,00 sempre.</summary>
public sealed class FreteExpresso : ICalculadoraDeFrete
{
    public decimal Calcular(decimal valorPedido) => 45m;
}

/// <summary>
/// Checkout que usa SEMPRE o frete padrão: pede a implementação pela chave com <see cref="FromKeyedServicesAttribute"/>.
/// </summary>
public sealed class ServicoDeCheckout([FromKeyedServices(ChavesFrete.Padrao)] ICalculadoraDeFrete frete)
{
    public decimal TotalComFrete(decimal valorPedido) => valorPedido + frete.Calcular(valorPedido);
}
