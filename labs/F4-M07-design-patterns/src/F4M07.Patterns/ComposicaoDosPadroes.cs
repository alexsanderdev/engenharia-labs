using F4M07.Patterns.Checkout;
using F4M07.Patterns.Frete;
using F4M07.Patterns.Notificacoes;
using F4M07.Patterns.Pagamentos;
using F4M07.Patterns.Pagamentos.PagaFacil;
using F4M07.Patterns.Precos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace F4M07.Patterns;

/// <summary>
/// Composition root dos padrões. É AQUI (e só aqui) que se decide qual implementação atende cada contrato,
/// em que ordem os decorators e as validações ficam e qual estratégia responde por cada chave.
/// </summary>
public static class ComposicaoDosPadroes
{
    /// <summary>Registra tudo. Pronto — chama os métodos abaixo.</summary>
    public static IServiceCollection AddPadroesDoOrderFlow(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddLogging();
        services.AddOptions();

        return services
            .AddFrete()
            .AddPrecos()
            .AddPagamentos()
            .AddNotificacoes()
            .AddCheckout();
    }

    /// <summary>
    /// STRATEGY via keyed services: cada <see cref="ICalculadoraDeFrete"/> registrada como keyed Singleton com a chave da
    /// sua modalidade (<see cref="ModalidadeFrete"/>); <see cref="ICotadorDeFrete"/> Singleton construído com TODAS as
    /// estratégias keyed: <c>sp.GetKeyedServices&lt;ICalculadoraDeFrete&gt;(KeyedService.AnyKey)</c>.
    /// </summary>
    public static IServiceCollection AddFrete(this IServiceCollection services)
    {
        services.AddKeyedSingleton<ICalculadoraDeFrete, FreteEconomico>(ModalidadeFrete.Economico);
        services.AddKeyedSingleton<ICalculadoraDeFrete, FreteExpresso>(ModalidadeFrete.Expresso);
        services.AddKeyedSingleton<ICalculadoraDeFrete, RetiradaNaLoja>(ModalidadeFrete.Retirada);
        services.AddSingleton<ICotadorDeFrete>(sp =>
            new CotadorDeFrete(sp.GetKeyedServices<ICalculadoraDeFrete>(KeyedService.AnyKey)));
        return services;
    }

    /// <summary>
    /// DECORATOR: <see cref="ServicoDePrecosDoCatalogo"/> Singleton (concreto, para os testes contarem as consultas) e
    /// <see cref="IServicoDePrecos"/> apontando para ele; depois <c>Decorar</c> com cache e, POR FORA, com log
    /// (log por fora = toda chamada é logada, inclusive as que o cache respondeu).
    /// </summary>
    public static IServiceCollection AddPrecos(this IServiceCollection services)
    {
        services.AddOptions<OpcoesDoCacheDePrecos>();
        services.AddSingleton<ServicoDePrecosDoCatalogo>();
        services.AddSingleton<IServicoDePrecos>(sp => sp.GetRequiredService<ServicoDePrecosDoCatalogo>());
        services.Decorar<IServicoDePrecos, ServicoDePrecosComCache>();
        services.Decorar<IServicoDePrecos, ServicoDePrecosComLog>();
        return services;
    }

    /// <summary>
    /// ADAPTER: <see cref="IGatewayDePagamento"/> → <see cref="PagaFacilAdapter"/> (Singleton). O <see cref="PagaFacilClient"/>
    /// usa <c>TryAddSingleton</c> com um "sandbox" que aprova tudo, para o teste poder registrar o seu antes.
    /// </summary>
    public static IServiceCollection AddPagamentos(this IServiceCollection services)
    {
        services.TryAddSingleton(_ => new PagaFacilClient(
            _ => new PagaFacilChargeResponse { Status = 0, AuthCode = "SANDBOX" }));
        services.AddSingleton<IGatewayDePagamento, PagaFacilAdapter>();
        return services;
    }

    /// <summary>FACTORY: <see cref="IFabricaDeNotificacoes"/> Singleton + <see cref="OpcoesDeNotificacao"/>.</summary>
    public static IServiceCollection AddNotificacoes(this IServiceCollection services)
    {
        services.AddOptions<OpcoesDeNotificacao>();
        services.AddSingleton<IFabricaDeNotificacoes, FabricaDeNotificacoes>();
        return services;
    }

    /// <summary>
    /// CHAIN OF RESPONSIBILITY + DI explícita: <see cref="ICatalogoParaCheckout"/> (TryAdd, <see cref="CatalogoEmMemoria"/>),
    /// as 4 validações como <see cref="IValidacaoDeCheckout"/> Scoped NESTA ordem — CarrinhoNaoVazio, QuantidadesPositivas,
    /// ProdutosAtivos, EstoqueSuficiente —, o pipeline Scoped, <see cref="OpcoesDeCheckout"/> e a calculadora Scoped.
    /// </summary>
    public static IServiceCollection AddCheckout(this IServiceCollection services)
    {
        services.TryAddSingleton<ICatalogoParaCheckout, CatalogoEmMemoria>();
        services.AddScoped<IValidacaoDeCheckout, CarrinhoNaoVazio>();
        services.AddScoped<IValidacaoDeCheckout, QuantidadesPositivas>();
        services.AddScoped<IValidacaoDeCheckout, ProdutosAtivos>();
        services.AddScoped<IValidacaoDeCheckout, EstoqueSuficiente>();
        services.AddScoped<PipelineDeValidacaoDoCheckout>();
        services.AddOptions<OpcoesDeCheckout>();
        services.AddScoped<CalculadoraDeTotalDoPedido>();
        return services;
    }
}
