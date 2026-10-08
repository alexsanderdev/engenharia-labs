using F4M05.Api.Comum;
using F4M05.Api.Repositories;
using F4M05.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratamentoDeErros>();

// ---------- CÓDIGO INICIAL: camadas horizontais ----------
// TODO (Passo 5): quando o último controller sumir, apague estas linhas (e AddControllers/MapControllers).
builder.Services.AddControllers();
builder.Services.AddSingleton<IPedidoRepository, PedidoRepository>();
builder.Services.AddSingleton<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

// ---------- Plataforma das fatias (pronta em Comum/) ----------
// TODO (Passo 3): registre aqui o BancoEmMemoria como singleton.
// Cada fatia em Features/ se registra sozinha (handlers + validadores + endpoints), por reflexão.
builder.Services.AddFeatures(typeof(Program).Assembly);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapControllers();
app.MapFeatures();

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
