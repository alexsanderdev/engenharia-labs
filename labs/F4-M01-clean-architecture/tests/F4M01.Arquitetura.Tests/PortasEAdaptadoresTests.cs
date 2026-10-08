using Mono.Cecil;
using NetArchTest.Rules;

namespace F4M01.Arquitetura.Tests;

/// <summary>Ports &amp; Adapters: a Application declara as portas, a Infrastructure fornece os adaptadores.</summary>
public sealed class PortasEAdaptadoresTests
{
    private static readonly Type[] Portas =
        [.. Camadas.ApplicationAssembly.GetTypes().Where(t => t.IsInterface && t.Namespace == "F4M01.Application.Abstracoes")];

    [Fact]
    public void Portas_ExistemNaApplication()
    {
        Portas.Select(p => p.Name).ShouldBe(["IPedidoRepository", "IProdutoRepository", "IRelogio"], ignoreOrder: true);
    }

    [Fact]
    public void CadaPorta_TemUmAdaptadorNaInfrastructure()
    {
        var tiposDaInfra = Camadas.InfrastructureAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .ToList();

        var semAdaptador = Portas
            .Where(porta => !tiposDaInfra.Any(porta.IsAssignableFrom))
            .Select(p => p.Name)
            .ToList();

        semAdaptador.ShouldBeEmpty($"Portas sem adaptador na Infrastructure: {string.Join(", ", semAdaptador)}");
    }

    [Fact]
    public void Adaptadores_SoExistemNaInfrastructure()
    {
        // Ninguém no Domain, na Application ou na Api implementa porta "por conta própria"
        // (ex.: um relógio com DateTime.UtcNow escondido num endpoint).
        var implementacoesForaDaInfra = new[] { Camadas.DomainAssembly, Camadas.ApplicationAssembly, Camadas.ApiAssembly }
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true } && Portas.Any(p => p.IsAssignableFrom(t)))
            .Select(t => t.FullName)
            .ToList();

        implementacoesForaDaInfra.ShouldBeEmpty();
    }

    [Fact]
    public void Entidades_NaoTemSetterPublico()
    {
        Types.InAssembly(Camadas.DomainAssembly)
            .That()
            .AreClasses()
            .Should()
            .MeetCustomRule(new SemSetterPublico())
            .GetResult()
            .DeveSerRespeitada("estado do domínio muda por métodos com intenção, nunca por setter público");
    }

    /// <summary>Regra customizada (Mono.Cecil, que o NetArchTest usa por baixo): nenhuma propriedade com set público.</summary>
    private sealed class SemSetterPublico : ICustomRule
    {
        public bool MeetsRule(TypeDefinition type) => !type.Properties.Any(p => p.SetMethod is { IsPublic: true });
    }
}
