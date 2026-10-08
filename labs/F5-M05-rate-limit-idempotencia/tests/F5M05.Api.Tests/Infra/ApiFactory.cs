using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using F5M05.Api.Autenticacao;
using F5M05.Api.Catalogo;
using F5M05.Api.Pedidos;
using F5M05.Api.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace F5M05.Api.Tests.Infra;

/// <summary>
/// A API em memória, configurada para testes DETERMINÍSTICOS:
/// <list type="bullet">
/// <item>Os limitadores do .NET (System.Threading.RateLimiting) não aceitam <c>TimeProvider</c>:
/// a reposição é feita por timer interno com o relógio real. Por isso, aqui todas as janelas e
/// períodos são de 1 hora — durante um teste NADA é reposto e o resultado depende só da contagem
/// de requisições, nunca do relógio. (O comportamento da reposição é testado à parte, em
/// <c>AlgoritmosTests</c>, com <c>AutoReplenishment = false</c> + <c>TryReplenish()</c>.)</item>
/// <item>Limites altos por padrão; cada teste baixa só o que quer exercitar.</item>
/// <item><see cref="FakeTimeProvider"/> no lugar do relógio: a expiração das Idempotency-Keys avança quando o teste manda.</item>
/// <item>Header <c>X-Test-Ip</c> simula o IP do cliente (o TestServer não tem conexão de rede).</item>
/// </list>
/// </summary>
public sealed class ApiFactory(Action<LimitesDeTaxaOptions>? limites = null, Action<IServiceCollection>? servicos = null)
    : WebApplicationFactory<Program>
{
    public const string ChaveClienteA = "chave-de-teste-cliente-a";
    public const string ChaveClienteB = "chave-de-teste-cliente-b";
    public const string ChaveBackoffice = "chave-de-teste-backoffice";

    public FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(s =>
        {
            s.AddSingleton<TimeProvider>(Relogio);
            s.AddSingleton<IStartupFilter, IpDeTesteStartupFilter>();
            s.PostConfigure<ApiKeysOptions>(o => o.Chaves = new()
            {
                [ChaveClienteA] = "cliente-a",
                [ChaveClienteB] = "cliente-b",
                [ChaveBackoffice] = "backoffice",
            });
            s.PostConfigure<LimitesDeTaxaOptions>(o =>
            {
                var umaHora = TimeSpan.FromHours(1);
                o.Pedidos = new() { Limite = 1_000, Janela = umaHora };
                o.Consultas = new() { Limite = 1_000, Janela = umaHora, Segmentos = 4 };
                o.Catalogo = new() { Capacidade = 1_000, TokensPorPeriodo = 1, Periodo = umaHora };
                o.Relatorios = new() { Limite = 1, Fila = 0 };
                limites?.Invoke(o);
            });
            servicos?.Invoke(s);
        });
    }

    public ICatalogo Catalogo => Services.GetRequiredService<ICatalogo>();
    public IRepositorioDePedidos Pedidos => Services.GetRequiredService<IRepositorioDePedidos>();

    public HttpClient ClienteComChave(string? chave, string? ip = null)
    {
        var client = CreateClient();
        if (chave is not null) client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.Cabecalho, chave);
        if (ip is not null) client.DefaultRequestHeaders.Add(IpDeTesteStartupFilter.Cabecalho, ip);
        return client;
    }

    private sealed class IpDeTesteStartupFilter : IStartupFilter
    {
        public const string Cabecalho = "X-Test-Ip";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((contexto, proximo) =>
            {
                if (contexto.Request.Headers.TryGetValue(Cabecalho, out var ip))
                    contexto.Connection.RemoteIpAddress = IPAddress.Parse(ip.ToString());
                return proximo(contexto);
            });
            next(app);
        };
    }
}

public static class Http
{
    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static object NovoPedido(int quantidade = 1, Guid? produtoId = null) =>
        new { itens = new[] { new { produtoId = produtoId ?? ProdutosConhecidos.TecladoId, quantidade } } };

    public static Task<HttpResponseMessage> PostarPedidoAsync(this HttpClient client, object corpo, string? chaveDeIdempotencia)
    {
        var requisicao = new HttpRequestMessage(HttpMethod.Post, "/pedidos") { Content = JsonContent.Create(corpo) };
        if (chaveDeIdempotencia is not null) requisicao.Headers.Add("Idempotency-Key", chaveDeIdempotencia);
        return client.SendAsync(requisicao, Ct);
    }

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage resposta) =>
        await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);

    public static async Task<Guid> IdDoPedidoAsync(this HttpResponseMessage resposta) =>
        (await resposta.JsonAsync()).GetProperty("id").GetGuid();
}
