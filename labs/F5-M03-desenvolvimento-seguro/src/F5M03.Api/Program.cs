using System.Text.Json.Serialization;
using F5M03.Api.Clientes;
using F5M03.Api.Dominio;
using F5M03.Api.Pedidos;
using F5M03.Api.Produtos;
using F5M03.Api.Seguranca;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(); // ProblemDetails (RFC 9457) com traceId
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProdutoRepositorio, ProdutoRepositorioEmMemoria>();
builder.Services.AddSingleton<IClienteRepositorio, ClienteRepositorioEmMemoria>();
builder.Services.AddSingleton<IPedidoRepositorio, PedidoRepositorioEmMemoria>();

builder.AddSegurancaDeBorda(); // Seguranca/SegurancaExtensions.cs

var app = builder.Build();

app.UseSegurancaDeBorda(); // Seguranca/SegurancaExtensions.cs

app.MapProdutoEndpoints();
app.MapClienteEndpoints();
app.MapPedidoEndpoints();

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
