using F5M03.Api.Tests.Infra;
using Microsoft.AspNetCore.Mvc.Testing;
using static F5M03.Api.Tests.Infra.Dados;

namespace F5M03.Api.Tests;

/// <summary>
/// Ataque 6 — cabeçalhos de segurança ausentes (OWASP API8:2023). Para uma API JSON:
/// nosniff, CSP que não permite carregar nada, e HSTS em produção (só em HTTPS).
/// </summary>
public sealed class CabecalhosTests
{
    [Theory]
    [InlineData("/produtos")]
    [InlineData("/rota-que-nao-existe")]
    [InlineData("/produtos?ordenarPor=xyz")]
    public async Task TodaResposta_TemNosniffECspDeApi(string caminho)
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        var resposta = await client.GetAsync(caminho, Ct);

        resposta.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"]);
        var csp = resposta.Headers.GetValues("Content-Security-Policy").Single();
        csp.ShouldContain("default-src 'none'");
        csp.ShouldContain("frame-ancestors 'none'");
    }

    [Fact]
    public async Task Producao_EmHttps_EnviaHstsDeUmAno()
    {
        await using var api = new ApiFactory("Production");
        using var client = api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://api.orderflow.test") });

        var resposta = await client.GetAsync("/produtos", Ct);

        resposta.Headers.TryGetValues("Strict-Transport-Security", out var hsts).ShouldBeTrue("produção em HTTPS deve mandar HSTS");
        hsts!.Single().ShouldContain("max-age=31536000");
    }

    [Fact]
    public async Task Desenvolvimento_NaoEnviaHsts()
    {
        await using var api = new ApiFactory("Development");
        using var client = api.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://api.orderflow.test") });

        var resposta = await client.GetAsync("/produtos", Ct);

        resposta.Headers.Contains("Strict-Transport-Security").ShouldBeFalse("HSTS em dev 'gruda' no navegador do desenvolvedor");
    }
}

/// <summary>
/// Ataque 7 — CORS permissivo. Refletir qualquer Origin COM credenciais deixa qualquer site
/// ler respostas autenticadas do usuário logado. A política aceita só origens configuradas.
/// </summary>
public sealed class CorsTests : TesteDeApi
{
    private const string OrigemMaliciosa = "https://evil.example";

    [Fact]
    public async Task Preflight_DeOrigemDesconhecida_NaoAutoriza()
    {
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/pedidos");
        preflight.Headers.Add("Origin", OrigemMaliciosa);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

        var resposta = await Client.SendAsync(preflight, Ct);

        resposta.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
        resposta.Headers.Contains("Access-Control-Allow-Credentials").ShouldBeFalse();
    }

    [Fact]
    public async Task GetComCredenciais_DeOrigemDesconhecida_NaoLiberaLeituraNoNavegador()
    {
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/produtos");
        requisicao.Headers.Add("Origin", OrigemMaliciosa);
        requisicao.Headers.Add("Cookie", "orderflow_session=sessao-de-teste");

        var resposta = await Client.SendAsync(requisicao, Ct);

        resposta.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse(
            "sem ACAO o navegador bloqueia a leitura da resposta pelo site malicioso");
    }

    [Fact]
    public async Task Preflight_DaOrigemDoApp_NaoLiberaMetodoNaoUsado()
    {
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/pedidos");
        preflight.Headers.Add("Origin", "https://app.orderflow.test");
        preflight.Headers.Add("Access-Control-Request-Method", "DELETE");

        var resposta = await Client.SendAsync(preflight, Ct);

        var metodos = resposta.Headers.TryGetValues("Access-Control-Allow-Methods", out var valores) ? string.Join(",", valores) : "";
        metodos.ShouldNotContain("DELETE", Case.Insensitive, "menor privilégio: só os métodos que o front usa");
    }
}
