using F2M06.TestesUnitarios.Aplicacao;
using F2M06.TestesUnitarios.Dominio;

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
    public static ICatalogoDeProdutos Com(params Produto[] produtos) =>
        throw new NotImplementedException(
            "TODO (Passo 5): Substitute.For<ICatalogoDeProdutos>() e, para cada produto, " +
            "catalogo.ObterPorIdAsync(produto.Id, Arg.Any<CancellationToken>()).Returns(...).");
}
