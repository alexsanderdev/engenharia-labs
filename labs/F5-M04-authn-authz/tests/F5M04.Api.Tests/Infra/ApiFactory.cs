using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using F5M04.Api.Seguranca;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace F5M04.Api.Tests.Infra;

/// <summary>
/// API de verdade em memória. Troca SÓ duas coisas: o relógio (FakeTimeProvider) e a origem das chaves
/// públicas (o emissor de teste). Emissor/audiência vêm de configuração com valores DIFERENTES do appsettings,
/// para garantir que a API lê <see cref="AutenticacaoOptions"/> em vez de valores fixos no código.
/// </summary>
public sealed class ApiFactory(string ambiente = "Development") : WebApplicationFactory<Program>
{
    /// <summary>Data fixa: o resultado dos testes não depende do relógio da máquina.</summary>
    public static readonly DateTimeOffset Inicio = new(2030, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid AdminId = Guid.Parse("adadadad-0000-0000-0000-000000000001");
    public static readonly Guid Ana = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid Bruno = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public FakeTimeProvider Relogio { get; } = new(Inicio);
    public EmissorDeTokensDeTeste Emissor => field ??= new EmissorDeTokensDeTeste(Relogio);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(ambiente);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Autenticacao:Emissor"] = EmissorDeTokensDeTeste.Emissor,
            ["Autenticacao:Audiencia"] = EmissorDeTokensDeTeste.Audiencia,
            ["Autenticacao:ToleranciaDeRelogio"] = "00:00:30",
        }));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Relogio);
            services.RemoveAll<IChavesDeAssinatura>();
            services.AddSingleton<IChavesDeAssinatura>(Emissor);
        });
    }

    // ---------- Helpers de identidade (cada um devolve um HttpClient já autenticado) ----------

    public static EspecificacaoDeToken TokenDeAdmin => new()
    {
        Sub = AdminId.ToString(),
        Papeis = [Papeis.Admin],
        Escopos = $"{Escopos.ProdutosEscrita} {Escopos.PedidosLeitura} {Escopos.PedidosEscrita}",
    };

    public static EspecificacaoDeToken TokenDeCliente(Guid id, string escopos = $"{Escopos.PedidosLeitura} {Escopos.PedidosEscrita}") => new()
    {
        Sub = id.ToString(),
        Papeis = [Papeis.Cliente],
        Escopos = escopos,
    };

    public HttpClient Anonimo() => CreateClient();

    public HttpClient ComToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient ComoAdmin() => ComToken(Emissor.Emitir(TokenDeAdmin));

    public HttpClient ComoCliente(Guid id, string escopos = $"{Escopos.PedidosLeitura} {Escopos.PedidosEscrita}") =>
        ComToken(Emissor.Emitir(TokenDeCliente(id, escopos)));

    /// <summary>Token de cliente emitido há 1 hora com validade de 5 minutos: expirado muito além da tolerância.</summary>
    public HttpClient ComTokenExpirado(Guid? id = null) =>
        ComToken(Emissor.Emitir(TokenDeCliente(id ?? Ana) with { EmitidoHa = TimeSpan.FromHours(1) }));
}

public static class HttpTestExtensions
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Confere status e o corpo ProblemDetails (application/problem+json) comum a toda resposta de erro.</summary>
    public static async Task<JsonElement> DeveSerProblemAsync(this HttpResponseMessage response, HttpStatusCode status)
    {
        response.StatusCode.ShouldBe(status, await DescreverAsync(response));
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        json.GetProperty("status").GetInt32().ShouldBe((int)status);
        return json;
    }

    /// <summary>401 + desafio <c>WWW-Authenticate: Bearer ...</c> (RFC 6750 §3). Devolve o valor do desafio.</summary>
    public static async Task<string> DeveSer401ComDesafioBearerAsync(this HttpResponseMessage response)
    {
        await response.DeveSerProblemAsync(HttpStatusCode.Unauthorized);
        var desafio = response.Headers.WwwAuthenticate.ShouldHaveSingleItem();
        desafio.Scheme.ShouldBe("Bearer");
        return desafio.ToString();
    }

    public static async Task<T> LerAsync<T>(this HttpResponseMessage response, HttpStatusCode status)
    {
        response.StatusCode.ShouldBe(status, await DescreverAsync(response));
        return (await response.Content.ReadFromJsonAsync<T>(Ct))!;
    }

    /// <summary>Mensagem de falha útil: corpo + WWW-Authenticate (o error_description diz POR QUE o token foi recusado).</summary>
    private static async Task<string> DescreverAsync(HttpResponseMessage response) =>
        $"WWW-Authenticate: {response.Headers.WwwAuthenticate} | Corpo: {await response.Content.ReadAsStringAsync(Ct)}";
}
