using F3M04.EfCore.Pedidos;
using F3M04.EfCore.Persistencia;
using Microsoft.Extensions.DependencyInjection;

namespace F3M04.EfCore.Tests;

/// <summary>Passo 9: ciclo de vida. DbContext não é thread-safe: um por escopo (requisição).</summary>
public sealed class InjecaoDeDependenciaTests
{
    [Fact]
    public void AddOrderFlowPersistencia_DbContextEServico_SaoScoped()
    {
        var services = new ServiceCollection().AddOrderFlowPersistencia("Server=nao-conecta;Database=X;Trusted_Connection=True");

        services.Single(d => d.ServiceType == typeof(OrderFlowDbContext)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(PedidoService)).Lifetime.ShouldBe(ServiceLifetime.Scoped);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var escopo1 = provider.CreateScope();
        using var escopo2 = provider.CreateScope();

        var a = escopo1.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        escopo1.ServiceProvider.GetRequiredService<OrderFlowDbContext>().ShouldBeSameAs(a);
        escopo2.ServiceProvider.GetRequiredService<OrderFlowDbContext>().ShouldNotBeSameAs(a);
        escopo1.ServiceProvider.GetRequiredService<PedidoService>().ShouldNotBeNull();
    }
}
