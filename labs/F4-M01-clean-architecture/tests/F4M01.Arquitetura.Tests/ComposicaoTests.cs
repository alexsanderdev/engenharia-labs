using F4M01.Application.Abstracoes;
using F4M01.Application.Pedidos;
using F4M01.Domain.Pedidos;
using F4M01.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace F4M01.Arquitetura.Tests;

/// <summary>
/// O composition root da Api monta tudo? Nenhum teste aqui abre conexão com o banco:
/// resolver serviços e construir o modelo do EF Core não toca no SQL Server.
/// </summary>
public sealed class ComposicaoTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Theory]
    [InlineData(typeof(CriarPedidoHandler))]
    [InlineData(typeof(ListarPedidosDoClienteHandler))]
    public void Api_ResolveCasosDeUso(Type casoDeUso)
    {
        using var scope = factory.Services.CreateScope();

        Should.NotThrow(() => scope.ServiceProvider.GetRequiredService(casoDeUso));
    }

    [Theory]
    [InlineData(typeof(IPedidoRepository))]
    [InlineData(typeof(IProdutoRepository))]
    [InlineData(typeof(IRelogio))]
    public void Api_ResolvePortasComAdaptadoresDaInfrastructure(Type porta)
    {
        using var scope = factory.Services.CreateScope();

        var adaptador = scope.ServiceProvider.GetService(porta);

        adaptador.ShouldNotBeNull($"Nenhum adaptador registrado para {porta.Name}");
        adaptador.GetType().Assembly.ShouldBe(Camadas.InfrastructureAssembly);
    }

    [Fact]
    public void ModeloEf_MapeiaTotalComPrecisaoDeDinheiro()
    {
        // Vale com ou sem atributos no domínio: o que importa é o modelo final que o EF Core monta.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        var total = db.Model.FindEntityType(typeof(Pedido))!.FindProperty(nameof(Pedido.Total))!;

        total.GetPrecision().ShouldBe(18);
        total.GetScale().ShouldBe(2);
    }
}
