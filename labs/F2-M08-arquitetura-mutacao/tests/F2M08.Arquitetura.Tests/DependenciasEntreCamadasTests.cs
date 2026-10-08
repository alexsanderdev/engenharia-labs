using F2M08.Domain.Repositorios;
using NetArchTest.Rules;

namespace F2M08.Arquitetura.Tests;

/// <summary>
/// Regra de ouro das camadas: as dependências apontam para DENTRO.
/// Infrastructure → Application → Domain. O domínio não conhece ninguém.
/// </summary>
public sealed class DependenciasEntreCamadasTests
{
    [Fact]
    public void Domain_NaoDependeDeApplicationNemDeInfrastructure()
    {
        Types.InAssembly(Camadas.DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Camadas.Application, Camadas.Infrastructure)
            .GetResult()
            .DeveSerRespeitada("o Domain não pode depender de Application nem de Infrastructure");
    }

    [Fact]
    public void Domain_NaoDependeDeEntityFrameworkCore()
    {
        Types.InAssembly(Camadas.DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(Camadas.EntityFrameworkCore)
            .GetResult()
            .DeveSerRespeitada("o Domain não pode depender de EF Core (mapeamento é detalhe de Infrastructure)");
    }

    [Fact]
    public void Application_NaoDependeDeInfrastructure()
    {
        Types.InAssembly(Camadas.ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(Camadas.Infrastructure)
            .GetResult()
            .DeveSerRespeitada("a Application depende de abstrações (interfaces do Domain), nunca de classes da Infrastructure");
    }

    [Fact]
    public void Application_NaoDependeDeEntityFrameworkCore()
    {
        Types.InAssembly(Camadas.ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(Camadas.EntityFrameworkCore)
            .GetResult()
            .DeveSerRespeitada("a Application não pode depender de EF Core");
    }

    [Fact]
    public void ImplementacoesDeRepositorio_MoramNaInfrastructure()
    {
        Types.InAssemblies([Camadas.DomainAssembly, Camadas.ApplicationAssembly, Camadas.InfrastructureAssembly])
            .That()
            .ImplementInterface(typeof(IPedidoRepository))
            .Or()
            .ImplementInterface(typeof(IProdutoRepository))
            .Should()
            .ResideInNamespace(Camadas.Infrastructure)
            .GetResult()
            .DeveSerRespeitada("classes de repositório (implementações) ficam na Infrastructure");
    }
}
