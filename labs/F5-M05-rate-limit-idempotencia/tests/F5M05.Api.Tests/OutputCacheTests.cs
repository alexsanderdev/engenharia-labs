using System.Net;
using System.Net.Http.Json;
using F5M05.Api.Catalogo;
using F5M05.Api.Tests.Infra;
using static F5M05.Api.Tests.Infra.Http;

namespace F5M05.Api.Tests;

/// <summary>
/// Parte (c), Passo 6 — output cache no GET /catalogo com invalidação por tag.
/// <see cref="ICatalogo.Leituras"/> conta as idas ao "banco": se não subiu, a resposta veio do cache.
/// </summary>
public sealed class OutputCacheTests
{
    [Fact]
    public async Task Catalogo_SegundaLeitura_VemDoCacheSemTocarNoBanco()
    {
        await using var api = new ApiFactory();
        using var anonimo = api.CreateClient();

        var primeira = await anonimo.GetStringAsync("/catalogo", Ct);
        var segunda = await anonimo.GetStringAsync("/catalogo", Ct);

        segunda.ShouldBe(primeira);
        api.Catalogo.Leituras.ShouldBe(1);
    }

    [Fact]
    public async Task Catalogo_CategoriasDiferentes_SaoEntradasDiferentesDoCache()
    {
        await using var api = new ApiFactory();
        using var anonimo = api.CreateClient();

        await anonimo.GetStringAsync("/catalogo?categoria=perifericos", Ct);
        await anonimo.GetStringAsync("/catalogo?categoria=monitores", Ct);
        await anonimo.GetStringAsync("/catalogo?categoria=perifericos", Ct);

        api.Catalogo.Leituras.ShouldBe(2);
    }

    [Fact]
    public async Task AtualizarPreco_InvalidaATag_ProximaLeituraTrazOPrecoNovo()
    {
        await using var api = new ApiFactory();
        using var anonimo = api.CreateClient();
        using var backoffice = api.ClienteComChave(ApiFactory.ChaveBackoffice);
        await anonimo.GetStringAsync("/catalogo", Ct);
        await anonimo.GetStringAsync("/catalogo?categoria=perifericos", Ct);
        await anonimo.GetStringAsync("/catalogo?categoria=perifericos", Ct);
        api.Catalogo.Leituras.ShouldBe(2, "antes da escrita, as duas variações estão em cache");

        var atualizacao = await backoffice.PutAsJsonAsync($"/catalogo/{ProdutosConhecidos.TecladoId}/preco", new { preco = 149.90m }, Ct);
        atualizacao.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var depois = await anonimo.GetFromJsonAsync<Produto[]>("/catalogo?categoria=perifericos", Ct);
        depois!.Single(p => p.Id == ProdutosConhecidos.TecladoId).Preco.ShouldBe(149.90m, "a tag derrubou todas as variações");
        api.Catalogo.Leituras.ShouldBe(3);
    }
}
