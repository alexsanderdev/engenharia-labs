using System.Net;
using System.Net.Http.Json;
using F5M03.Api.Seguranca;
using F5M03.Api.Tests.Infra;
using static F5M03.Api.Tests.Infra.Dados;

namespace F5M03.Api.Tests;

/// <summary>
/// Ataque 5 — dados sensíveis em log (OWASP Top 10 A09; LGPD). Log é lido por muita gente
/// (suporte, fornecedores de observabilidade, backups) e vive muito tempo. Senha, token e
/// CPF completo não entram; CPF e e-mail entram MASCARADOS quando ajudam a operação.
/// </summary>
public sealed class LogsTests : TesteDeApi
{
    [Fact]
    public async Task Cadastro_NaoLogaSenhaNemCpfCompleto()
    {
        var resposta = await Client.PostAsJsonAsync("/clientes", NovoCliente(), Ct);
        resposta.StatusCode.ShouldBe(HttpStatusCode.Created);

        var logs = Api.Logs.GetSnapshot().Select(r => r.TextoCompleto()).ToList();
        logs.ShouldNotBeEmpty();
        logs.ShouldAllBe(l => !l.Contains(SenhaValida), "senha nunca vai para o log");
        logs.ShouldAllBe(l => !l.Contains(CpfSoDigitos) && !l.Contains(CpfValido), "CPF completo nunca vai para o log");
    }

    [Fact]
    public async Task Cadastro_LogaOCpfMascarado()
    {
        await Client.PostAsJsonAsync("/clientes", NovoCliente(), Ct);

        Api.Logs.GetSnapshot().Select(r => r.TextoCompleto())
            .ShouldContain(l => l.Contains("***.***.***-25"), "o cadastro deixa rastro útil para o suporte, mascarado");
    }

    [Fact]
    public async Task Requisicao_ComAuthorizationECookie_NaoLogaOsSegredos()
    {
        const string token = "eyJ-token-de-teste-que-nao-pode-vazar";
        const string sessao = "sessao-de-teste-que-nao-pode-vazar";
        using var requisicao = new HttpRequestMessage(HttpMethod.Get, "/produtos");
        requisicao.Headers.Add("Authorization", $"Bearer {token}");
        requisicao.Headers.Add("Cookie", $"orderflow_session={sessao}");
        requisicao.Headers.Add("X-Api-Key", "chave-de-teste-que-nao-pode-vazar");

        await Client.SendAsync(requisicao, Ct);

        var logs = Api.Logs.GetSnapshot().Select(r => r.TextoCompleto()).ToList();
        logs.ShouldContain(l => l.Contains("/produtos"), "o log de acesso continua existindo");
        logs.ShouldAllBe(l => !l.Contains(token) && !l.Contains(sessao) && !l.Contains("chave-de-teste-que-nao-pode-vazar"));
    }
}

/// <summary>Passo 5 — a função de mascaramento, isolada.</summary>
public sealed class MascaramentoTests
{
    [Theory]
    [InlineData("52998224725", "***.***.***-25")]
    [InlineData("529.982.247-25", "***.***.***-25")]
    [InlineData("123", "***")]
    [InlineData("", "***")]
    [InlineData(null, "***")]
    public void Cpf_MantemSoOsDoisUltimosDigitos(string? cpf, string esperado) =>
        Mascaramento.Cpf(cpf).ShouldBe(esperado);

    [Theory]
    [InlineData("ana.souza@exemplo.com", "a***@exemplo.com")]
    [InlineData("b@x.io", "b***@x.io")]
    [InlineData("sem-arroba", "***")]
    [InlineData("@exemplo.com", "***")]
    [InlineData(null, "***")]
    public void Email_MantemPrimeiraLetraEDominio(string? email, string esperado) =>
        Mascaramento.Email(email).ShouldBe(esperado);
}
