using F2M06.TestesUnitarios.Aplicacao;
using F2M06.TestesUnitarios.Dominio;
using NSubstitute;

namespace F2M06.TestesUnitarios.Tests.Dubles;

/// <summary>
/// STUB com NSubstitute: só ALIMENTA o caso de uso com dados. Stubs nunca são verificados
/// (nada de <c>catalogo.Received().ObterPorIdAsync(...)</c>): consultar o catálogo é detalhe de implementação.
/// </summary>
public static class CatalogoStub
{
    /// <summary>
    /// Catálogo que conhece exatamente os <paramref name="produtos"/> informados.
    /// Qualquer outro id devolve <c>null</c> (produto não encontrado).
    /// </summary>
    public static ICatalogoDeProdutos Com(params Produto[] produtos)
    {
        var catalogo = Substitute.For<ICatalogoDeProdutos>();

        // Padrão explícito: id desconhecido → null. (O NSubstitute já devolveria null para uma classe sealed,
        // mas deixar explícito documenta a intenção e não depende de detalhe da biblioteca.)
        catalogo.ObterPorIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Produto?>(null));

        foreach (var produto in produtos)
        {
            catalogo.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<Produto?>(produto));
        }

        return catalogo;
    }
}
