using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MP3.Notificacoes.Tests.Infra;
using MP3.Notificacoes.Worker.Mensageria;
using MP3.Notificacoes.Worker.Telemetria;

namespace MP3.Notificacoes.Tests;

/// <summary>Health checks (live/ready) e o trace continuando o da API.</summary>
[Collection(ColecaoAmbiente.Nome)]
public sealed class SaudeETelemetriaTests(AmbienteFixture ambiente)
{
    [Fact]
    public async Task HealthChecks_LiveSempre_ReadyDependeDoConsumidor()
    {
        await using var worker = new WorkerFactory(ambiente).Iniciar();
        using var http = worker.CreateClient();

        (await http.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
        var pronto = await http.GetAsync("/health/ready");
        pronto.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await pronto.Content.ReadAsStringAsync()).ShouldBe("Healthy");

        await worker.Services.GetRequiredService<ConsumidorDeNotificacoes>().StopAsync(CancellationToken.None);

        (await http.GetAsync("/health/ready")).StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await http.GetAsync("/health/live")).StatusCode.ShouldBe(HttpStatusCode.OK, "liveness não depende do consumidor");
    }

    [Fact]
    public async Task Processamento_ContinuaOTraceDaApi()
    {
        await ambiente.DrenarAsync();
        var spans = new ConcurrentQueue<Activity>();
        using var ouvinte = new ActivityListener
        {
            ShouldListenTo = fonte => fonte.Name == Telemetria.Nome,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = spans.Enqueue,
        };
        ActivitySource.AddActivityListener(ouvinte);
        await using var worker = new WorkerFactory(ambiente).Iniciar();
        var evento = Novo.Pedido();

        // O "span da API" que publicou o evento (W3C traceparent: 00-traceId-spanId-flags).
        var traceDaApi = ActivityTraceId.CreateRandom();
        var spanDaApi = ActivitySpanId.CreateRandom();
        await ambiente.PublicarAsync(Novo.Mensagem(evento, traceparent: $"00-{traceDaApi}-{spanDaApi}-01"));

        await Esperas.Eventualmente(
            () => spans.Any(s => s.Kind == ActivityKind.Consumer && (string?)s.GetTagItem("messaging.message.id") == evento.EventoId.ToString()),
            "span de processamento");
        var processamento = spans.First(s => s.Kind == ActivityKind.Consumer && (string?)s.GetTagItem("messaging.message.id") == evento.EventoId.ToString());
        processamento.TraceId.ShouldBe(traceDaApi, "mesmo trace da API: uma única visualização");
        processamento.ParentSpanId.ShouldBe(spanDaApi);
        processamento.GetTagItem("mp3.desfecho").ShouldBe(nameof(Desfecho.Notificada));

        var envio = spans.Single(s => s.ParentSpanId == processamento.SpanId);
        envio.DisplayName.ShouldBe("notificar teste");
        envio.TraceId.ShouldBe(traceDaApi);
    }
}
