using System.Net.Http.Json;
using System.Text.Json;
using F6M08.Consistencia.Consistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace F6M08.Consistencia.Tests.Infra;

/// <summary>
/// A API em memória com relógio falso. Os consumidores (processador de comandos, projetor, reconciliação)
/// rodam como BackgroundService de verdade; o que eles "veem" depende de quanto o teste avança o relógio.
/// </summary>
public sealed class ApiFactory(Action<ConsistenciaOptions>? opcoes = null) : WebApplicationFactory<Program>
{
    public FakeTimeProvider Relogio { get; } = new(Cenario.Inicio);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(s =>
        {
            s.AddSingleton<TimeProvider>(Relogio);
            s.PostConfigure<ConsistenciaOptions>(o =>
            {
                o.AtrasoDoProcessamento = TimeSpan.FromSeconds(2);
                o.AtrasoDaProjecao = TimeSpan.FromSeconds(3);
                o.EsperaMaximaDaLeitura = TimeSpan.FromSeconds(5);
                o.ToleranciaDaReconciliacao = TimeSpan.FromMinutes(5);
                o.IntervaloDaReconciliacao = TimeSpan.FromHours(1);
                opcoes?.Invoke(o);
            });
        });
    }
}

public static class Http
{
    public static async Task<JsonElement> JsonAsync(this HttpResponseMessage resposta) =>
        await resposta.Content.ReadFromJsonAsync<JsonElement>(Eventos.Ct);

    /// <summary>
    /// O que um cliente de API assíncrona faz: consulta o Location até sair de "Processando".
    /// Polling com limite curto (nada de sleep fixo "para dar tempo").
    /// </summary>
    public static async Task<JsonElement> AguardarOperacaoAsync(this HttpClient cliente, Uri location)
    {
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(Eventos.Ct);
        limite.CancelAfter(Eventos.LimiteDeSeguranca);
        while (true)
        {
            var corpo = await (await cliente.GetAsync(location, limite.Token)).JsonAsync();
            if (corpo.GetProperty("status").GetString() != "Processando") return corpo;
            await Task.Delay(TimeSpan.FromMilliseconds(10), limite.Token);
        }
    }
}
