using System.Net.Http.Headers;
using F5M06.Pagamentos.Checkout;
using F5M06.Pagamentos.Gateway;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace F5M06.Pagamentos;

public static class DependencyInjection
{
    /// <summary>Nome do HttpClient do gateway no IHttpClientFactory.</summary>
    public const string NomeCliente = "gateway-pagamento";

    /// <summary>
    /// Registra a integração com o gateway:
    /// <list type="number">
    /// <item>options <see cref="GatewayPagamentoOptions"/> com <paramref name="configurar"/> e validação DataAnnotations;</item>
    /// <item><see cref="TimeProvider.System"/> (TryAdd: testes trocam por FakeTimeProvider), <see cref="UltimoStatusConhecido"/> singleton,
    /// <see cref="CorrelacaoEApiKeyHandler"/> transiente e <see cref="CheckoutService"/>;</item>
    /// <item>typed client <c>AddHttpClient&lt;IGatewayPagamento, GatewayPagamentoClient&gt;(NomeCliente, ...)</c> com BaseAddress das options,
    /// <c>Timeout = Timeout.InfiniteTimeSpan</c> (quem manda é o pipeline) e Accept JSON;</item>
    /// <item><c>AddHttpMessageHandler&lt;CorrelacaoEApiKeyHandler&gt;()</c> ANTES do resilience handler;</item>
    /// <item><c>AddResilienceHandler(ResilienciaGateway.NomePipeline, ...)</c> chamando <see cref="ResilienciaGateway.Configurar"/>
    /// com as options lidas de <c>context.GetOptions&lt;GatewayPagamentoOptions&gt;()</c>.</item>
    /// </list>
    /// </summary>
    public static IHttpClientBuilder AddGatewayPagamento(this IServiceCollection services, Action<GatewayPagamentoOptions> configurar)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configurar);

        services.AddOptions<GatewayPagamentoOptions>()
            .Configure(configurar)
            .ValidateDataAnnotations();

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<UltimoStatusConhecido>();
        services.TryAddTransient<CorrelacaoEApiKeyHandler>();
        services.TryAddTransient<CheckoutService>();

        var builder = services
            .AddHttpClient<IGatewayPagamento, GatewayPagamentoClient>(NomeCliente, (sp, http) =>
            {
                var o = sp.GetRequiredService<IOptions<GatewayPagamentoOptions>>().Value;
                http.BaseAddress = o.BaseAddress;
                http.Timeout = Timeout.InfiniteTimeSpan;
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler<CorrelacaoEApiKeyHandler>();

        builder.AddResilienceHandler(ResilienciaGateway.NomePipeline, (pipeline, contexto) =>
        {
            contexto.EnableReloads<GatewayPagamentoOptions>();
            ResilienciaGateway.Configurar(pipeline, contexto.GetOptions<GatewayPagamentoOptions>());
        });

        return builder;
    }
}
