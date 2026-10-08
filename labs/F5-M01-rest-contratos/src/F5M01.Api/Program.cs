using System.Diagnostics;
using F5M01.Api.Dominio;
using F5M01.Api.Http;

var builder = WebApplication.CreateBuilder(args);

// Todo erro sai como ProblemDetails (RFC 9457) com traceId — inclusive 404 de rota e 405 de método (UseStatusCodePages).
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier);
        ctx.ProblemDetails.Instance ??= ctx.HttpContext.Request.Path;
    });

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICatalogo, CatalogoEmMemoria>();
builder.Services.AddSingleton<IPedidoRepositorio, PedidoRepositorioEmMemoria>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// A API REST nova.
app.MapPedidosEndpoints();

// A API RPC legada (Legado/RpcEndpoints.cs) deixou de ser mapeada: os clientes migraram para /pedidos.
// Em produção, você a manteria por um período de depreciação anunciado (módulo 5.02) antes de remover.

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
