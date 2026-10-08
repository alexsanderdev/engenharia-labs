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
    public static IServiceCollection AddFrete(this IServiceCollection services) =>
        throw new NotImplementedException(
            "TODO: services.AddKeyedSingleton<ICalculadoraDeFrete, FreteEconomico>(ModalidadeFrete.Economico) (e as outras duas) + " +
            "services.AddSingleton<ICotadorDeFrete>(sp => new CotadorDeFrete(sp.GetKeyedServices<ICalculadoraDeFrete>(KeyedService.AnyKey))).");

    /// <summary>
    /// DECORATOR: <see cref="ServicoDePrecosDoCatalogo"/> Singleton (concreto, para os testes contarem as consultas) e
    /// <see cref="IServicoDePrecos"/> apontando para ele; depois <c>Decorar</c> com cache e, POR FORA, com log
    /// (log por fora = toda chamada é logada, inclusive as que o cache respondeu). Registre também
    /// <c>AddOptions&lt;OpcoesDoCacheDePrecos&gt;()</c>.
    /// </summary>
    public static IServiceCollection AddPrecos(this IServiceCollection services) =>
        throw new NotImplementedException(
            $"TODO: AddSingleton<{nameof(ServicoDePrecosDoCatalogo)}>(), AddSingleton<IServicoDePrecos>(sp => sp.GetRequiredService<...>()), " +
            "Decorar<IServicoDePrecos, ServicoDePrecosComCache>(), Decorar<IServicoDePrecos, ServicoDePrecosComLog>().");

    /// <summary>
    /// ADAPTER: <see cref="IGatewayDePagamento"/> → <see cref="PagaFacilAdapter"/> (Singleton). O <see cref="PagaFacilClient"/>
    /// usa <c>TryAddSingleton</c> com um "sandbox" que aprova tudo
    /// (<c>new PagaFacilChargeResponse { Status = 0, AuthCode = "SANDBOX" }</c>), para o teste poder registrar o seu antes.
    /// </summary>
    public static IServiceCollection AddPagamentos(this IServiceCollection services) =>
        throw new NotImplementedException(
            $"TODO: TryAddSingleton(_ => new {nameof(PagaFacilClient)}(...sandbox...)) + AddSingleton<IGatewayDePagamento, PagaFacilAdapter>().");

    /// <summary>FACTORY: <see cref="IFabricaDeNotificacoes"/> Singleton + <c>AddOptions&lt;OpcoesDeNotificacao&gt;()</c>.</summary>
    public static IServiceCollection AddNotificacoes(this IServiceCollection services) =>
        throw new NotImplementedException($"TODO: AddOptions<{nameof(OpcoesDeNotificacao)}>() + AddSingleton<IFabricaDeNotificacoes, FabricaDeNotificacoes>().");

    /// <summary>
    /// CHAIN OF RESPONSIBILITY + DI explícita: <see cref="ICatalogoParaCheckout"/> (TryAdd, <see cref="CatalogoEmMemoria"/>),
    /// as 4 validações como <see cref="IValidacaoDeCheckout"/> Scoped NESTA ordem — CarrinhoNaoVazio, QuantidadesPositivas,
    /// ProdutosAtivos, EstoqueSuficiente —, o pipeline Scoped, <c>AddOptions&lt;OpcoesDeCheckout&gt;()</c> e a calculadora Scoped.
    /// </summary>
    public static IServiceCollection AddCheckout(this IServiceCollection services) =>
        throw new NotImplementedException(
            $"TODO: a ordem de AddScoped<IValidacaoDeCheckout, ...> É a ordem da corrente. Depois {nameof(PipelineDeValidacaoDoCheckout)} e {nameof(CalculadoraDeTotalDoPedido)}.");
}
