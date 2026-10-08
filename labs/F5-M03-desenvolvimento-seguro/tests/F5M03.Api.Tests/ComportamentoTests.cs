using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using F5M03.Api.Dominio;
using F5M03.Api.Tests.Infra;
using static F5M03.Api.Tests.Infra.Dados;

namespace F5M03.Api.Tests;

/// <summary>
/// Caracterização: o que a API FAZ para o cliente honesto. Já passam no starter e
/// precisam CONTINUAR passando depois de cada correção — segurança não pode quebrar o caminho feliz.
/// </summary>
public sealed class ComportamentoTests : TesteDeApi
{
    [Fact]
    public async Task Produtos_Listar_Retorna200SoComAtivosOrdenadosPorNome()
    {
        var resposta = await Client.GetAsync("/produtos", Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var nomes = (await resposta.JsonAsync()).EnumerateArray().Select(p => p.GetProperty("nome").GetString()).ToList();
        nomes.ShouldBe(["Monitor 27\"", "Mouse sem fio", "Teclado mecânico"]);
    }

    [Fact]
    public async Task Produtos_ListarOrdenadoPorPreco_Retorna200NaOrdemDoPreco()
    {
        var resposta = await Client.GetAsync("/produtos?ordenarPor=preco", Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var precos = (await resposta.JsonAsync()).EnumerateArray().Select(p => p.GetProperty("preco").GetDecimal()).ToList();
        precos.ShouldBe([99.90m, 199.90m, 1299.00m]);
    }

    [Fact]
    public async Task Clientes_CadastroValido_Retorna201ComLocationEDadosBasicos()
    {
        var resposta = await Client.PostAsJsonAsync("/clientes", NovoCliente(), Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var corpo = await resposta.JsonAsync();
        var id = corpo.GetProperty("id").GetGuid();
        corpo.GetProperty("nome").GetString().ShouldBe("Ana Souza");
        corpo.GetProperty("email").GetString().ShouldBe("ana.souza@exemplo.com");
        resposta.Headers.Location!.ToString().ShouldBe($"/clientes/{id}");
        Api.Clientes.Obter(id).ShouldNotBeNull().SenhaHash.ShouldNotContain(SenhaValida);
    }

    [Fact]
    public async Task Pedidos_CriarValido_Retorna201ComTotalCalculadoPeloCatalogo()
    {
        var resposta = await Client.PostAsJsonAsync("/pedidos", NovoPedido(Guid.NewGuid(), ProdutosConhecidos.TecladoId, 2), Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);
        var corpo = await resposta.JsonAsync();
        corpo.GetProperty("total").GetDecimal().ShouldBe(399.80m);
        corpo.GetProperty("status").GetString().ShouldBe("Created");

        var obtido = await Client.GetAsync(resposta.Headers.Location, Ct);
        obtido.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await obtido.JsonAsync()).GetProperty("total").GetDecimal().ShouldBe(399.80m);
    }

    [Fact]
    public async Task Upload_PngPequeno_Retorna204EGuardaAImagem()
    {
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];
        using var conteudo = new ByteArrayContent(png);
        conteudo.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        var resposta = await Client.PostAsync($"/produtos/{ProdutosConhecidos.MouseId}/imagem", conteudo, Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        Api.Produtos.Obter(ProdutosConhecidos.MouseId)!.Imagem.ShouldBe(png);
    }

    [Fact]
    public async Task Cors_PreflightDaOrigemDoApp_PermiteAOrigem()
    {
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/pedidos");
        preflight.Headers.Add("Origin", "https://app.orderflow.test");
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var resposta = await Client.SendAsync(preflight, Ct);

        resposta.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(["https://app.orderflow.test"]);
    }
}
