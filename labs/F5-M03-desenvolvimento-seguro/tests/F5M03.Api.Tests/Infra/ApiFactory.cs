using System.Net.Http.Json;
using System.Text.Json;
using F5M03.Api.Dominio;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace F5M03.Api.Tests.Infra;

/// <summary>
/// A API de verdade em memória. Ambiente padrão: <b>Production</b> — o que importa é o que
/// o atacante vê em produção, não a página de erro amigável de Development.
/// Logs vão para um <see cref="FakeLogCollector"/> para os testes inspecionarem.
/// </summary>
public sealed class ApiFactory(string ambiente = "Production") : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(ambiente);
        builder.ConfigureLogging(logging => logging.AddFakeLogging());
    }

    public FakeLogCollector Logs => Services.GetRequiredService<FakeLogCollector>();
    public IClienteRepositorio Clientes => Services.GetRequiredService<IClienteRepositorio>();
    public IPedidoRepositorio Pedidos => Services.GetRequiredService<IPedidoRepositorio>();
    public IProdutoRepositorio Produtos => Services.GetRequiredService<IProdutoRepositorio>();
}

public static class Dados
{
    /// <summary>CPF de teste com dígitos verificadores válidos.</summary>
    public const string CpfValido = "529.982.247-25";
    public const string CpfSoDigitos = "52998224725";
    public const string SenhaValida = "S3nh@-Muito-Secreta!";

    public static object NovoCliente(string? nome = "Ana Souza", string? email = "ana.souza@exemplo.com",
        string? cpf = CpfValido, string? senha = SenhaValida) => new { nome, email, cpf, senha };

    public static object NovoPedido(Guid clienteId, Guid produtoId, int quantidade = 1) =>
        new { clienteId, itens = new[] { new { produtoId, quantidade } } };

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage resposta) =>
        await resposta.Content.ReadFromJsonAsync<JsonElement>(Ct);

    /// <summary>Nomes das propriedades de um objeto JSON (case-insensitive na comparação dos testes).</summary>
    public static IReadOnlyList<string> Propriedades(this JsonElement objeto) =>
        [.. objeto.EnumerateObject().Select(p => p.Name)];

    public static bool TemPropriedade(this JsonElement objeto, string nome) =>
        objeto.EnumerateObject().Any(p => string.Equals(p.Name, nome, StringComparison.OrdinalIgnoreCase));

    /// <summary>Chaves de "errors" de um ValidationProblem.</summary>
    public static IReadOnlyList<string> CamposComErro(this JsonElement problema) =>
        problema.TryGetProperty("errors", out var erros) ? [.. erros.EnumerateObject().Select(e => e.Name)] : [];

    /// <summary>Texto completo de um registro de log: mensagem formatada, valores estruturados e exceção.</summary>
    public static string TextoCompleto(this FakeLogRecord registro) =>
        string.Join(" | ",
            new[] { registro.Message, registro.Exception?.ToString() }
                .Concat(registro.StructuredState?.Select(kv => $"{kv.Key}={kv.Value}") ?? []));
}

/// <summary>Base dos testes de API: uma factory (e um "banco" em memória) por teste.</summary>
public abstract class TesteDeApi : IAsyncDisposable
{
    protected TesteDeApi(string ambiente = "Production")
    {
        Api = new ApiFactory(ambiente);
        Client = Api.CreateClient();
    }

    protected ApiFactory Api { get; }
    protected HttpClient Client { get; }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Api.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
