using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace F1M06.Hosting.Tests;

public class ValidacaoTests
{
    [Fact]
    public void EncontrarProblemas_ComposicaoDoOrderFlow_NaoTemProblemas()
    {
        var services = new ServiceCollection().AddOrderFlow(new ConfigurationBuilder().Build());

        ValidacaoDoContainer.EncontrarProblemas(services).ShouldBeEmpty();
    }

    [Fact]
    public void EncontrarProblemas_DependenciaCativa_ApontaOSingletonEOScoped()
    {
        var services = new ServiceCollection().AddComDependenciaCativa();

        var problemas = ValidacaoDoContainer.EncontrarProblemas(services);

        problemas.Count.ShouldBe(1);
        problemas[0].ShouldContain(nameof(CacheDeCatalogo));
        problemas[0].ShouldContain(nameof(IContextoDaOperacao));
    }

    [Fact]
    public void OrderFlowHost_ComDependenciaCativa_FalhaAoConstruir()
    {
        // O host valida o container no Build: o erro aparece no deploy, não na primeira requisição.
        Should.Throw<AggregateException>(() => OrderFlowHost.Criar(configurarServicos: s => s.AddSingleton<CacheDeCatalogo>()));
    }

    [Fact]
    public void OrderFlowHost_ComposicaoCorreta_ConstroiERegistraOWorker()
    {
        using var host = OrderFlowHost.Criar();

        host.Services.GetServices<IHostedService>().OfType<LimpezaDePedidosExpirados>().Count().ShouldBe(1);
    }
}
