using System.Net;
using F5M03.Api.Dominio;
using F5M03.Api.Tests.Infra;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using static F5M03.Api.Tests.Infra.Dados;

namespace F5M03.Api.Tests;

/// <summary>
/// Ataque 4 — mensagens de erro vazando detalhes (OWASP API8:2023, Security Misconfiguration).
/// Stack trace, nome de servidor, SQL e tipo de exceção são mapa do tesouro para o atacante.
/// O cliente recebe 500 genérico com traceId; o detalhe vai para o LOG do servidor.
/// </summary>
public sealed class ErrosTests
{
    private sealed class RepositorioQueFalha : IProdutoRepositorio
    {
        public IReadOnlyList<Produto> ListarAtivos(string colunaDeOrdenacao) =>
            throw new InvalidOperationException(
                "Login failed for user 'orderflow_app'. Server=sql-prod-01.orderflow.internal; SQL: SELECT * FROM dbo.Produtos");

        public Produto? Obter(Guid id) => null;
    }

    [Fact]
    public async Task ErroInesperado_Retorna500ProblemDetailsGenericoComTraceId()
    {
        await using var api = new ApiFactory();
        await using var comFalha = api.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<IProdutoRepositorio>(new RepositorioQueFalha())));
        using var client = comFalha.CreateClient();

        var resposta = await client.GetAsync("/produtos", Ct);

        resposta.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        resposta.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var texto = await resposta.Content.ReadAsStringAsync(Ct);
        texto.ShouldNotContain("sql-prod-01");
        texto.ShouldNotContain("SELECT");
        texto.ShouldNotContain("orderflow_app");
        texto.ShouldNotContain("InvalidOperationException");
        texto.ShouldNotContain(" at ", Case.Sensitive, "nada de stack trace");
        (await resposta.JsonAsync()).TemPropriedade("traceId").ShouldBeTrue("o suporte precisa correlacionar com o log");
        resposta.Headers.GetValues("X-Content-Type-Options").ShouldBe(["nosniff"], "cabeçalhos de segurança valem também na resposta de erro");
    }

    [Fact]
    public async Task ErroInesperado_DetalheCompletoVaiParaOLogDoServidor()
    {
        await using var api = new ApiFactory();
        await using var comFalha = api.WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => s.AddSingleton<IProdutoRepositorio>(new RepositorioQueFalha())));
        using var client = comFalha.CreateClient();

        await client.GetAsync("/produtos", Ct);

        var logs = comFalha.Services.GetRequiredService<Microsoft.Extensions.Logging.Testing.FakeLogCollector>().GetSnapshot();
        logs.ShouldContain(r => r.Level == Microsoft.Extensions.Logging.LogLevel.Error && r.Exception is InvalidOperationException,
            "quem investiga o incidente precisa da exceção completa — no log, não na resposta");
    }

    [Fact]
    public async Task OrdenacaoInvalida_NaoDevolveOSqlNaResposta()
    {
        await using var api = new ApiFactory();
        using var client = api.CreateClient();

        var resposta = await client.GetAsync("/produtos?ordenarPor=Preco%20DESC%3B--", Ct);

        ((int)resposta.StatusCode).ShouldBeLessThan(500, "entrada inválida do cliente é 4xx, não 500");
        var texto = await resposta.Content.ReadAsStringAsync(Ct);
        texto.ShouldNotContain("SELECT");
        texto.ShouldNotContain("sql-prod-01");
    }
}
