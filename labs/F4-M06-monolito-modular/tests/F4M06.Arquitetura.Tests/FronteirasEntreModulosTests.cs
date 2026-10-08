using NetArchTest.Rules;

namespace F4M06.Arquitetura.Tests;

/// <summary>
/// Regra de ouro do monólito modular: um módulo só conversa com outro pelo CONTRATO público
/// (<c>.Contracts</c>). Tipos internos, entidades e DbContext de outro módulo são proibidos.
/// </summary>
public sealed class FronteirasEntreModulosTests
{
    [Theory]
    [InlineData("Catalogo")]
    [InlineData("Pedidos")]
    [InlineData("Clientes")]
    public void Modulo_NaoUsaTiposDaImplementacaoDeOutroModulo(string nome)
    {
        var modulo = Modulos.PorNome(nome);
        string[] proibidos = [.. Modulos.OutrosAlemDe(nome).SelectMany(m => m.TiposDaImplementacao())];

        Types.InAssembly(modulo.Implementacao)
            .ShouldNot()
            .HaveDependencyOnAny(proibidos)
            .GetResult()
            .DeveSerRespeitada($"{nome} só pode usar o .Contracts dos outros módulos (nunca entidades, DbContext ou classes da implementação)");
    }

    [Theory]
    [InlineData("Catalogo")]
    [InlineData("Pedidos")]
    [InlineData("Clientes")]
    public void Modulo_NaoAcessaDbContextDeOutroModulo(string nome)
    {
        var modulo = Modulos.PorNome(nome);
        string[] dbContextsDosOutros = [.. Modulos.OutrosAlemDe(nome).SelectMany(m => m.DbContexts()).Select(t => t.FullName!)];

        Types.InAssembly(modulo.Implementacao)
            .ShouldNot()
            .HaveDependencyOnAny(dbContextsDosOutros)
            .GetResult()
            .DeveSerRespeitada($"{nome} não pode ler nem gravar nas tabelas de outro módulo (DbContext alheio)");
    }

    [Theory]
    [InlineData("Catalogo")]
    [InlineData("Pedidos")]
    [InlineData("Clientes")]
    public void Contracts_NaoDependemDeImplementacaoNemDeInfraestrutura(string nome)
    {
        var modulo = Modulos.PorNome(nome);
        string[] proibidos =
        [
            .. Modulos.Todos.SelectMany(m => m.TiposDaImplementacao()),
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
        ];

        Types.InAssembly(modulo.Contratos)
            .ShouldNot()
            .HaveDependencyOnAny(proibidos)
            .GetResult()
            .DeveSerRespeitada($"o contrato de {nome} só tem interfaces, DTOs e eventos: nada de implementação, EF Core ou ASP.NET");
    }

    [Fact]
    public void Shared_NaoConheceNenhumModulo()
    {
        string[] tiposDosModulos =
        [
            .. Modulos.Todos.SelectMany(m => m.TiposDaImplementacao()),
            .. Modulos.Todos.SelectMany(m => m.Contratos.GetTypes()).Where(t => !t.IsNested).Select(t => t.FullName!),
        ];

        Types.InAssembly(Modulos.Shared)
            .ShouldNot()
            .HaveDependencyOnAny(tiposDosModulos)
            .GetResult()
            .DeveSerRespeitada("o Shared é infraestrutura comum mínima: não conhece nenhum módulo");
    }
}
