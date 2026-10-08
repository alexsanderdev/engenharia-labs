using System.Diagnostics;
using F4M08.Api.Http;
using F4M08.Api.Infra;
using F4M08.Api.Pedidos;

var builder = WebApplication.CreateBuilder(args);

// ProblemDetails padronizado (RFC 9457) para TODA resposta de erro:
// erros de negócio (Result → ToProblem), exceções (GlobalExceptionHandler) e status sem corpo (UseStatusCodePages).
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
    {
        // traceId liga a resposta ao log/trace daquela requisição. Activity.Current existe quando há tracing (OpenTelemetry).
        ctx.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier);
        ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path;
    });
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICatalogo, CatalogoEmMemoria>();
builder.Services.AddSingleton<IPedidoRepositorio, PedidoRepositorioEmMemoria>();
builder.Services.AddScoped<PedidosCasosDeUso>();

var app = builder.Build();

// Ordem: o tratador de exceções vem primeiro para envolver todo o resto do pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapPedidoEndpoints();

// Endpoint de diagnóstico do LAB: simula um bug com dado sensível na mensagem da exceção.
// O teste garante que nada disso chega ao cliente. Nunca deixe algo assim em produção.
app.MapGet("/diagnostico/falha", IResult () =>
    throw new InvalidOperationException("Falha ao abrir conexão: Server=sql-prod;User Id=sa;Password=SenhaSecreta123"));

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
