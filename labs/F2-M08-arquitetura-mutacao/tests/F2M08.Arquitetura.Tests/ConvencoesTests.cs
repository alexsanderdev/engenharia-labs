using F2M08.Application.Abstracoes;
using NetArchTest.Rules;

namespace F2M08.Arquitetura.Tests;

/// <summary>Convenções de projeto que valem a pena automatizar: nomes, selamento e encapsulamento.</summary>
public sealed class ConvencoesTests
{
    [Fact]
    public void Handlers_TerminamComHandler()
    {
        Types.InAssembly(Camadas.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .HaveNameEndingWith("Handler")
            .GetResult()
            .DeveSerRespeitada("toda implementação de ICommandHandler<,> termina com \"Handler\"");
    }

    [Fact]
    public void Handlers_SaoSealed()
    {
        Types.InAssembly(Camadas.ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(ICommandHandler<,>))
            .Should()
            .BeSealed()
            .GetResult()
            .DeveSerRespeitada("handlers são sealed (ninguém herda caso de uso)");
    }

    [Fact]
    public void Entidades_NaoTemSetterPublico()
    {
        Types.InAssembly(Camadas.DomainAssembly)
            .That()
            .ResideInNamespace("F2M08.Domain.Entidades")
            .And()
            .AreClasses()
            .Should()
            .MeetCustomRule(new SemSetterPublico())
            .GetResult()
            .DeveSerRespeitada("entidades não expõem setter público: estado muda por métodos do domínio");
    }

    [Fact]
    public void Interfaces_ComecamComI()
    {
        Types.InAssemblies([Camadas.DomainAssembly, Camadas.ApplicationAssembly, Camadas.InfrastructureAssembly])
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult()
            .DeveSerRespeitada("interfaces começam com I");
    }
}
