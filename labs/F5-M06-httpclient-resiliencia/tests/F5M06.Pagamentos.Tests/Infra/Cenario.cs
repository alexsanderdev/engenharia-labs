using F5M06.Pagamentos.Checkout;
using F5M06.Pagamentos.Gateway;
using Microsoft.Extensions.DependencyInjection;

namespace F5M06.Pagamentos.Tests.Infra;

/// <summary>
/// Monta o container de DI como a aplicação faria (<c>AddGatewayPagamento</c>), apontando para o
/// <see cref="GatewayFake"/> e trocando o <see cref="TimeProvider"/> pelo <see cref="RelogioDeTeste"/>.
/// Padrões dos testes: retry SEM atraso (backoff zero), timeouts folgados e circuito que não abre,
/// para cada teste ligar só o comportamento que quer observar.
/// (Infra pronta: você não precisa alterar este arquivo.)
/// </summary>
public sealed class Cenario : IDisposable
{
    public GatewayFake Gateway { get; } = new();
    public RelogioDeTeste Relogio { get; } = new();
    public ServiceProvider Servicos { get; }

    public Cenario(Action<GatewayPagamentoOptions>? ajustar = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddGatewayPagamento(o =>
        {
            o.BaseAddress = Gateway.Url;
            o.ApiKey = Gateway.ApiKey;
            o.MaxRetentativas = 2;
            o.AtrasoBase = TimeSpan.Zero;
            o.AtrasoMaximo = TimeSpan.FromSeconds(30);
            o.TimeoutPorTentativa = TimeSpan.FromSeconds(5);
            o.TimeoutTotal = TimeSpan.FromSeconds(60);
            o.CircuitoTaxaDeFalhas = 1.0;
            o.CircuitoVazaoMinima = 1_000;
            o.CircuitoJanela = TimeSpan.FromMinutes(5);
            o.CircuitoDuracaoAberto = TimeSpan.FromSeconds(30);
            ajustar?.Invoke(o);
        });
        services.AddSingleton<TimeProvider>(Relogio);
        Servicos = services.BuildServiceProvider();
    }

    /// <summary>Typed client resolvido do container (transiente: um novo a cada acesso).</summary>
    public IGatewayPagamento Cliente => Servicos.GetRequiredService<IGatewayPagamento>();

    public CheckoutService Checkout => Servicos.GetRequiredService<CheckoutService>();

    public static SolicitacaoCobranca NovaCobranca(decimal valor = 150m) =>
        new(Guid.NewGuid(), valor, "BRL", "tok_teste_visa");

    public void Dispose()
    {
        Servicos.Dispose();
        Gateway.Dispose();
    }
}
