using NetArchTest.Rules;

namespace F4M01.Arquitetura.Tests;

/// <summary>
/// A Regra da Dependência (Robert C. Martin): dependências de código-fonte só apontam para DENTRO.
/// Api → Application → Domain; Infrastructure → Application → Domain. O Domain não conhece ninguém.
/// </summary>
public sealed class RegraDaDependenciaTests
{
    [Fact]
    public void Domain_NaoDependeDeNenhumaOutraCamada()
    {
        Types.InAssembly(Camadas.DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Camadas.Application, Camadas.Infrastructure, Camadas.Api)
            .GetResult()
            .DeveSerRespeitada("o Domain é o centro: não conhece Application, Infrastructure nem Api");
    }

    [Fact]
    public void Domain_NaoDependeDeFrameworks()
    {
        Types.InAssembly(Camadas.DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Camadas.EntityFrameworkCore, Camadas.AspNetCore)
            .GetResult()
            .DeveSerRespeitada("o Domain não depende de EF Core nem de ASP.NET Core (mapeamento é detalhe da Infrastructure)");
    }

    [Fact]
    public void Application_NaoDependeDeInfrastructureNemDaApi()
    {
        Types.InAssembly(Camadas.ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Camadas.Infrastructure, Camadas.Api)
            .GetResult()
            .DeveSerRespeitada("a Application fala com o mundo só por PORTAS (interfaces dela mesma)");
    }

    [Fact]
    public void Application_NaoDependeDeFrameworks()
    {
        Types.InAssembly(Camadas.ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Camadas.EntityFrameworkCore, Camadas.AspNetCore)
            .GetResult()
            .DeveSerRespeitada("casos de uso não conhecem DbContext nem HttpContext");
    }

    [Fact]
    public void Infrastructure_NaoDependeDaApi()
    {
        Types.InAssembly(Camadas.InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(Camadas.Api)
            .GetResult()
            .DeveSerRespeitada("a Infrastructure não conhece a borda HTTP");
    }

    [Fact]
    public void Api_ForaDoCompositionRoot_NaoConheceInfrastructureNemEfCore()
    {
        Types.InAssembly(Camadas.ApiAssembly)
            .That()
            .DoNotResideInNamespace("F4M01.Api.Composicao")
            .ShouldNot()
            .HaveDependencyOnAny(Camadas.Infrastructure, Camadas.EntityFrameworkCore)
            .GetResult()
            .DeveSerRespeitada("só F4M01.Api.Composicao liga a Infrastructure; Program e endpoints usam casos de uso");
    }

    [Fact]
    public void Api_NaoExpoeEntidadesDoDominio()
    {
        Types.InAssembly(Camadas.ApiAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("F4M01.Domain.Pedidos", "F4M01.Domain.Produtos")
            .GetResult()
            .DeveSerRespeitada("a Api recebe e devolve DTOs; entidades ficam atrás dos casos de uso");
    }
}
