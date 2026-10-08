using F6M08.Consistencia.Api;
using F6M08.Consistencia.Consistencia;
using F6M08.Consistencia.Dominio;
using F6M08.Consistencia.Fonte;
using F6M08.Consistencia.Mensageria;
using F6M08.Consistencia.Projecao;
using F6M08.Consistencia.Reconciliacao;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<ConsistenciaOptions>().BindConfiguration(ConsistenciaOptions.Secao);

// "Broker" de comandos (o assíncrono do 202) e "broker" de eventos (fonte → projeção).
builder.Services.AddSingleton(sp => new FilaComAtraso<ComandoCriarPedido>(
    sp.GetRequiredService<TimeProvider>(),
    sp.GetRequiredService<IOptions<ConsistenciaOptions>>().Value.AtrasoDoProcessamento));
builder.Services.AddSingleton(sp => new FilaComAtraso<EventoDePedido>(
    sp.GetRequiredService<TimeProvider>(),
    sp.GetRequiredService<IOptions<ConsistenciaOptions>>().Value.AtrasoDaProjecao));

// Lado de escrita (fonte da verdade) e lado de leitura (projeção).
builder.Services.AddSingleton<FonteDePedidos>();
builder.Services.AddSingleton<RegistroDeOperacoes>();
builder.Services.AddSingleton<ProjecaoResumoDoCliente>();
builder.Services.AddSingleton<ServicoDeConsulta>();
builder.Services.AddSingleton<Reconciliador>();

// Consumidores em segundo plano.
builder.Services.AddSingleton<Projetor>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<Projetor>());
builder.Services.AddHostedService<ProcessadorDeComandos>();
builder.Services.AddHostedService<ReconciliacaoPeriodica>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapPedidos();

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
