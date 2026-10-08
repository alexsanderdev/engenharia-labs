using System.Diagnostics;
using F5M01.Api.Dominio;
using F5M01.Api.Http;
using F5M01.Api.Legado;

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

// A API REST nova (TODO: Http/PedidosEndpoints.cs).
app.MapPedidosEndpoints();

// A API RPC legada — o "antes" do lab. Rode o projeto e experimente com o F5M01.Api.http.
// TODO (passo 6): quando a API REST estiver pronta, deixe de mapear o legado (o teste RpcLegado_NaoEstaMaisExposto cobra isso).
// Em produção, você a manteria por um período de depreciação anunciado (módulo 5.02) antes de remover.
app.MapRpcLegado();

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
