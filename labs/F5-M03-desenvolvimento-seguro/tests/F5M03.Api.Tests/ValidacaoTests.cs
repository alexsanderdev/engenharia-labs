using System.Net;
using System.Net.Http.Json;
using F5M03.Api.Dominio;
using F5M03.Api.Tests.Infra;
using static F5M03.Api.Tests.Infra.Dados;

namespace F5M03.Api.Tests;

/// <summary>
/// Ataque 3 — entrada sem validação. Toda entrada é hostil até prova em contrário:
/// tamanho, faixa, formato e allowlist, sempre no servidor. Resposta esperada: 400
/// <c>application/problem+json</c> com o campo em <c>errors</c>, e nada persistido.
/// </summary>
public sealed class ValidacaoTests : TesteDeApi
{
    public static TheoryData<string, object> CadastrosInvalidos() => new()
    {
        { "Nome", NovoCliente(nome: "") },
        { "Nome", NovoCliente(nome: new string('A', 101)) },
        { "Email", NovoCliente(email: "nao-e-email") },
        { "Email", NovoCliente(email: new string('a', 250) + "@exemplo.com") },
        { "Cpf", NovoCliente(cpf: "111.111.111-11") },
        { "Cpf", NovoCliente(cpf: "529.982.247-26") },
        { "Cpf", NovoCliente(cpf: "52998224725' OR 1=1 --") },
        { "Senha", NovoCliente(senha: "123456") },
        { "Senha", NovoCliente(senha: null) },
    };

    [Theory]
    [MemberData(nameof(CadastrosInvalidos))]
    public async Task Cadastro_Invalido_Retorna400ComOCampoENaoPersiste(string campo, object corpo)
    {
        var resposta = await Client.PostAsJsonAsync("/clientes", corpo, Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await resposta.JsonAsync()).CamposComErro().ShouldContain(c => c.Equals(campo, StringComparison.OrdinalIgnoreCase));
        Api.Clientes.Todos().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task CriarPedido_QuantidadeForaDaFaixa_Retorna400(int quantidade)
    {
        var resposta = await Client.PostAsJsonAsync("/pedidos", NovoPedido(Guid.NewGuid(), ProdutosConhecidos.TecladoId, quantidade), Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.JsonAsync()).CamposComErro().ShouldContain(c => c.Contains("Quantidade", StringComparison.OrdinalIgnoreCase));
        Api.Pedidos.Todos().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public async Task CriarPedido_SemItensOuComItensDemais_Retorna400(int quantidadeDeItens)
    {
        var itens = Enumerable.Range(0, quantidadeDeItens).Select(_ => new { produtoId = ProdutosConhecidos.MouseId, quantidade = 1 }).ToArray();

        var resposta = await Client.PostAsJsonAsync("/pedidos", new { clienteId = Guid.NewGuid(), itens }, Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.JsonAsync()).CamposComErro().ShouldContain(c => c.Equals("Itens", StringComparison.OrdinalIgnoreCase));
        Api.Pedidos.Todos().ShouldBeEmpty();
    }

    [Fact]
    public async Task CriarPedido_ProdutoInativo_Retorna400()
    {
        var resposta = await Client.PostAsJsonAsync("/pedidos", NovoPedido(Guid.NewGuid(), ProdutosConhecidos.WebcamInativaId), Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        Api.Pedidos.Todos().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("custoInterno")]           // coluna real, mas interna: vira oráculo de margem
    [InlineData("Nome; DROP TABLE Produtos")]
    [InlineData("xyz")]
    public async Task ListarProdutos_OrdenacaoForaDaAllowlist_Retorna400(string ordenarPor)
    {
        var resposta = await Client.GetAsync($"/produtos?ordenarPor={Uri.EscapeDataString(ordenarPor)}", Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await resposta.JsonAsync()).CamposComErro().ShouldContain(c => c.Equals("ordenarPor", StringComparison.OrdinalIgnoreCase));
    }
}
