using System.Net;
using System.Net.Http.Json;
using F5M03.Api.Dominio;
using F5M03.Api.Tests.Infra;
using static F5M03.Api.Tests.Infra.Dados;

namespace F5M03.Api.Tests;

/// <summary>
/// Ataque 2 — exposição excessiva de dados (OWASP API3:2023). O endpoint devolve a ENTIDADE
/// e o front "só mostra o que precisa". O atacante não usa o seu front: ele lê o JSON.
/// </summary>
public sealed class ExposicaoDeDadosTests : TesteDeApi
{
    [Fact]
    public async Task Cadastro_Resposta_NaoExpoeSenhaHashIsAdminNemCpfCompleto()
    {
        var resposta = await Client.PostAsJsonAsync("/clientes", NovoCliente(), Ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var texto = await resposta.Content.ReadAsStringAsync(Ct);
        var corpo = await resposta.JsonAsync();
        corpo.TemPropriedade("senhaHash").ShouldBeFalse();
        corpo.TemPropriedade("senha").ShouldBeFalse();
        corpo.TemPropriedade("isAdmin").ShouldBeFalse();
        texto.ShouldNotContain(CpfSoDigitos);
        texto.ShouldNotContain(CpfValido);
        texto.ShouldNotContain("PBKDF2");
    }

    [Fact]
    public async Task ObterCliente_Resposta_NaoExpoeSenhaHashNemCpfCompleto()
    {
        var criado = await Client.PostAsJsonAsync("/clientes", NovoCliente(), Ct);

        var resposta = await Client.GetAsync(criado.Headers.Location, Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.OK);
        var texto = await resposta.Content.ReadAsStringAsync(Ct);
        texto.ShouldNotContain("PBKDF2");
        texto.ShouldNotContain(CpfSoDigitos);
        (await resposta.JsonAsync()).TemPropriedade("isAdmin").ShouldBeFalse();
    }

    [Fact]
    public async Task ObterPedido_Resposta_NaoExpoeCustoNemObservacaoInterna()
    {
        var criado = await Client.PostAsJsonAsync("/pedidos", NovoPedido(Guid.NewGuid(), ProdutosConhecidos.TecladoId), Ct);
        criado.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await criado.JsonAsync()).TemPropriedade("custoTotal").ShouldBeFalse("nem na resposta do POST");

        var resposta = await Client.GetAsync(criado.Headers.Location, Ct);

        var corpo = await resposta.JsonAsync();
        corpo.TemPropriedade("custoTotal").ShouldBeFalse();
        corpo.TemPropriedade("observacaoInterna").ShouldBeFalse();
    }

    [Fact]
    public async Task ListarProdutos_Resposta_NaoExpoeCustoInterno()
    {
        var resposta = await Client.GetAsync("/produtos", Ct);

        var produtos = (await resposta.JsonAsync()).EnumerateArray().ToList();
        produtos.ShouldNotBeEmpty();
        produtos.ShouldAllBe(p => !p.TemPropriedade("custoInterno"));
        produtos.ShouldAllBe(p => !p.TemPropriedade("imagem"), "binário da imagem não pertence à listagem");
    }
}
