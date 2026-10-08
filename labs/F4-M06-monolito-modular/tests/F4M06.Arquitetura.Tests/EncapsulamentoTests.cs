using F4M06.Catalogo.Contracts;
using F4M06.Clientes.Contracts;
using F4M06.Shared.Modulos;
using NetArchTest.Rules;

namespace F4M06.Arquitetura.Tests;

/// <summary>
/// Dados e detalhes PRIVADOS por módulo: o compilador é o primeiro guarda da fronteira.
/// Se a entidade é <c>internal</c>, outro projeto nem consegue escrever o acesso proibido.
/// </summary>
public sealed class EncapsulamentoTests
{
    [Theory]
    [InlineData("Catalogo")]
    [InlineData("Pedidos")]
    [InlineData("Clientes")]
    public void Modulo_SoExpoeAClasseDoModulo(string nome)
    {
        var modulo = Modulos.PorNome(nome);

        Types.InAssembly(modulo.Implementacao)
            .That()
            .DoNotImplementInterface(typeof(IModule))
            .Should()
            .NotBePublic()
            .GetResult()
            .DeveSerRespeitada($"em {nome}, só a classe que implementa IModule é pública; o resto é internal");
    }

    [Fact]
    public void CadaModulo_TemUmDbContextProprioEInterno()
    {
        var problemas = new List<string>();
        foreach (var modulo in Modulos.Todos)
        {
            var contexts = modulo.DbContexts();
            if (contexts.Length != 1)
                problemas.Add($"{modulo.Nome}: esperado 1 DbContext, encontrados {contexts.Length}");
            problemas.AddRange(contexts.Where(t => t.IsPublic).Select(t => $"{modulo.Nome}: {t.FullName} é público"));
        }

        problemas.ShouldBeEmpty("cada módulo tem exatamente um DbContext, internal (dados privados do módulo)");
    }

    [Theory]
    [InlineData(typeof(ICatalogoApi), "Catalogo")]
    [InlineData(typeof(IClientesApi), "Clientes")]
    public void ContratoPublico_ImplementadoPorClasseInternaDoModuloDono(Type contrato, string dono)
    {
        var implementacoes = Modulos.Todos
            .SelectMany(m => m.Implementacao.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && contrato.IsAssignableFrom(t))
            .ToList();

        implementacoes.ShouldNotBeEmpty($"ninguém implementa {contrato.Name}");
        implementacoes.ShouldAllBe(
            t => t.Assembly == Modulos.PorNome(dono).Implementacao && !t.IsPublic,
            $"{contrato.Name} deve ser implementado por uma classe internal do módulo {dono}. " +
            $"Encontradas: {string.Join(", ", implementacoes.Select(t => $"{t.FullName} (public: {t.IsPublic})"))}");
    }
}
