using System.Xml.Linq;

namespace F4M06.Arquitetura.Tests;

/// <summary>
/// A fronteira mais barata de manter é a do <c>.csproj</c>: se o projeto de Pedidos nem referencia
/// a implementação do Catálogo, ninguém consegue usar um tipo de lá "só desta vez".
/// </summary>
public sealed class ReferenciasDeProjetoTests
{
    [Theory]
    [InlineData("Catalogo")]
    [InlineData("Pedidos")]
    [InlineData("Clientes")]
    public void ProjetoDoModulo_SoReferenciaSharedEContractsDeOutrosModulos(string nome)
    {
        var csproj = Path.Combine(RaizDoLab(), "src", "Modules", nome, $"F4M06.{nome}", $"F4M06.{nome}.csproj");
        File.Exists(csproj).ShouldBeTrue($"não achei {csproj}");

        var proibidas = XDocument.Load(csproj)
            .Descendants("ProjectReference")
            .Select(r => Path.GetFileNameWithoutExtension(r.Attribute("Include")!.Value.Replace('\\', '/')))
            .Where(projeto => projeto != "F4M06.Shared" && !projeto.EndsWith(".Contracts", StringComparison.Ordinal))
            .ToList();

        proibidas.ShouldBeEmpty($"F4M06.{nome}.csproj só pode referenciar F4M06.Shared e projetos .Contracts");
    }

    /// <summary>Sobe a partir da pasta de saída do teste até achar a pasta do lab (a que contém <c>src/Modules</c>).</summary>
    private static string RaizDoLab()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "Modules")))
                return dir.FullName;
        }

        throw new DirectoryNotFoundException("Pasta do lab (com src/Modules) não encontrada acima de " + AppContext.BaseDirectory);
    }
}
