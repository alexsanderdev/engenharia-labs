using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MP3.Notificacoes.Worker.Configuracao;
using MP3.Notificacoes.Worker.Inbox;
using MP3.Notificacoes.Worker.Mensageria;
using MP3.Notificacoes.Worker.Notificacoes;
using MP3.Notificacoes.Worker.Processamento;
using MP3.Notificacoes.Worker.Saude;

// Worker de Notificações do OrderFlow (MP3). É um worker: o trabalho está nos hosted services.
// O Kestrel existe só para as probes /health/live e /health/ready.
var builder = WebApplication.CreateBuilder(args);

// Graceful shutdown: quanto tempo o host espera os handlers em andamento ao receber SIGTERM.
// Deve ser MENOR que o prazo do orquestrador (Kubernetes: terminationGracePeriodSeconds, 30 s por padrão).
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(25));

builder.Services.AddOptions<ServiceBusOptions>().BindConfiguration(ServiceBusOptions.Secao).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<NotificacoesOptions>().BindConfiguration(NotificacoesOptions.Secao).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<SmtpOptions>().BindConfiguration(SmtpOptions.Secao).ValidateDataAnnotations();

builder.Services.AddSingleton(TimeProvider.System);

// UM ServiceBusClient por processo: é a conexão AMQP (cara, thread-safe). O container de DI o descarta no fim.
builder.Services.AddSingleton(sp =>
{
    var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("ServiceBus");
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("ConnectionStrings:ServiceBus não configurada.");
    return new ServiceBusClient(cs);
});

builder.Services.AddSingleton<InboxSql>();
builder.Services.AddSingleton<PoliticaDeRetry>();
builder.Services.AddSingleton<ProcessadorDePedidoCriado>();
builder.Services.AddSingleton<ReprocessadorDeDlq>();
builder.Services.AddSingleton<IObservadorDeProcessamento, ObservadorDeMetricas>();

// Strategy: cada canal é um INotificador; o seletor escolhe pelo canal preferido do cliente.
builder.Services.AddSingleton<INotificador, NotificadorConsole>();
builder.Services.AddSingleton<INotificador, NotificadorEmailSmtp>();
builder.Services.AddSingleton<INotificador, NotificadorSmsFake>();
builder.Services.AddSingleton<SeletorDeNotificador>();

// A ordem importa: hosted services sobem em sequência (inbox antes do consumidor) e param na ordem inversa.
builder.Services.AddHostedService<InicializadorDaInbox>();
builder.Services.AddSingleton<ConsumidorDeNotificacoes>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ConsumidorDeNotificacoes>());

builder.Services.AddHealthChecks()
    .AddCheck<InboxHealthCheck>("inbox", tags: ["ready"])
    .AddCheck<ConsumidorHealthCheck>("consumidor", tags: ["ready"]);

var app = builder.Build();

// Modo comando: `dotnet run -- reprocessar-dlq [maximo]` devolve a DLQ para a fila e sai (sem subir o consumidor).
if (args is ["reprocessar-dlq", ..])
{
    var maximo = args.Length > 1 && int.TryParse(args[1], out var m) ? m : 100;
    var total = await app.Services.GetRequiredService<ReprocessadorDeDlq>().ReprocessarAsync(maximo);
    Console.WriteLine($"{total} mensagem(ns) devolvida(s) da DLQ para a fila.");
    return;
}

// Liveness: o processo responde? (sem dependências — senão o orquestrador reinicia o pod porque o banco caiu).
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
// Readiness: consigo trabalhar agora? (inbox + consumidor).
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

await app.RunAsync();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
