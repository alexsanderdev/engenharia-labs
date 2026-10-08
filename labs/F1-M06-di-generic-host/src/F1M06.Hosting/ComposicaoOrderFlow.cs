using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace F1M06.Hosting;

/// <summary>
/// Composition root do OrderFlow organizado em extension methods: cada área registra o que é dela,
/// e o Program.cs só chama <see cref="AddOrderFlow"/>.
/// </summary>
public static class ComposicaoOrderFlow
{
    /// <summary>
    /// Catálogo:
    /// <list type="bullet">
    /// <item><see cref="IRepositorioProdutos"/> → <see cref="RepositorioProdutosEmMemoria"/> como <b>Singleton</b>.</item>
    /// <item><see cref="IContextoDaOperacao"/> → <see cref="ContextoDaOperacao"/> como <b>Scoped</b>.</item>
    /// <item><see cref="IServicoDePrecos"/> como <b>Scoped</b>, resolvendo para <see cref="ServicoDePrecosComCache"/>
    /// que DECORA um <see cref="ServicoDePrecos"/> (registre a classe concreta e monte o decorator com uma factory).</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddCatalogo(this IServiceCollection services)
    {
        throw new NotImplementedException("TODO: registre repositório (Singleton), contexto (Scoped) e o decorator de preços (Scoped, via factory)");
    }

    /// <summary>
    /// Frete com keyed services: <see cref="FretePadrao"/> na chave <see cref="ChavesFrete.Padrao"/> e
    /// <see cref="FreteExpresso"/> na chave <see cref="ChavesFrete.Expresso"/> (ambos Singleton: sem estado).
    /// <see cref="ServicoDeCheckout"/> como Scoped.
    /// </summary>
    public static IServiceCollection AddFrete(this IServiceCollection services)
    {
        throw new NotImplementedException("TODO: AddKeyedSingleton<ICalculadoraDeFrete, ...>(chave) para cada frete e AddScoped<ServicoDeCheckout>()");
    }

    /// <summary>
    /// Pedidos:
    /// <list type="bullet">
    /// <item><see cref="OpcoesPedidos"/> ligadas à seção <see cref="OpcoesPedidos.Secao"/> (<c>AddOptions().Bind(...)</c>).</item>
    /// <item><see cref="TimeProvider"/>: <see cref="TimeProvider.System"/> com <c>TryAddSingleton</c> (o teste pode trocar).</item>
    /// <item><see cref="IGeradorDeCodigoPedido"/> Singleton criado por FACTORY a partir das opções e do TimeProvider.</item>
    /// <item><see cref="IRepositorioPedidos"/> Singleton; <see cref="IServicoDePedidos"/> Scoped.</item>
    /// <item>O worker <see cref="LimpezaDePedidosExpirados"/> com <c>AddHostedService</c>.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddPedidos(this IServiceCollection services, IConfiguration configuration)
    {
        throw new NotImplementedException("TODO: Options (Bind), TryAddSingleton(TimeProvider.System), gerador via factory, repositório, serviço e AddHostedService");
    }

    /// <summary>Registra tudo: logging, catálogo, frete e pedidos.</summary>
    public static IServiceCollection AddOrderFlow(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();
        return services
            .AddCatalogo()
            .AddFrete()
            .AddPedidos(configuration);
    }
}
