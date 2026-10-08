using F4M05.Api.Comum;
using F4M05.Api.Infraestrutura;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TratamentoDeErros>();
builder.Services.AddSingleton<BancoEmMemoria>();

// Cada fatia se registra sozinha (handlers + validadores + endpoints), por reflexão.
builder.Services.AddFeatures(typeof(Program).Assembly);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapFeatures();

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
