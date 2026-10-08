using F4M06.Catalogo.Contracts;
using F4M06.Integracao.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;

namespace F4M06.Integracao.Tests;

/// <summary>
/// Comunicação SÍNCRONA entre módulos: quem precisa de produtos pede <see cref="ICatalogoApi"/> ao container
/// de DI e recebe a implementação interna do Catálogo — sem saber que ela existe.
/// </summary>
[Collection(ColecaoIntegracao.Nome)]
public sealed class ContratoSincronoTests(OrderFlowFixture fixture) : IntegracaoTestBase(fixture)
{
    [Fact]
    public async Task CatalogoApi_ResolvidaPeloContrato_DevolveResumoDosProdutosEmLote()
    {
        var cafe = await CriarProdutoAsync("Café 500g", 32.50m);
        var cha = await CriarProdutoAsync("Chá verde", 18m);
        await DesativarProdutoAsync(cha.Id);

        await using var scope = Fixture.Factory.Services.CreateAsyncScope();
        var catalogo = scope.ServiceProvider.GetRequiredService<ICatalogoApi>();

        var produtos = await catalogo.ObterProdutosAsync([cafe.Id, cha.Id, Guid.NewGuid()], Ct);

        produtos.Count.ShouldBe(2, "ids inexistentes não aparecem no resultado");
        produtos.ShouldContain(new ProdutoResumo(cafe.Id, "Café 500g", 32.50m, Ativo: true));
        produtos.ShouldContain(new ProdutoResumo(cha.Id, "Chá verde", 18m, Ativo: false));
    }
}
