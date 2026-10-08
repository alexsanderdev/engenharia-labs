using F5M05.Api.Autenticacao;
using F5M05.Api.Catalogo;
using F5M05.Api.Idempotencia;
using F5M05.Api.Pedidos;
using F5M05.Api.RateLimiting;
using F5M05.Api.Relatorios;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.TryAddSingleton(TimeProvider.System);

// Autenticação por API key (pronta) — o rate limit e a idempotência precisam saber QUEM é o cliente.
builder.Services.Configure<ApiKeysOptions>(builder.Configuration.GetSection(ApiKeysOptions.Secao));
builder.Services.AddAuthentication(ApiKeyAuthenticationHandler.Esquema)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.Esquema, null);
builder.Services.AddAuthorization();

// Domínio em memória (pronto)
builder.Services.AddSingleton<ICatalogo, CatalogoEmMemoria>();
builder.Services.AddSingleton<IRepositorioDePedidos, RepositorioDePedidosEmMemoria>();
builder.Services.AddSingleton<IGeradorDeRelatorio, GeradorDeRelatorio>();

// Rate limiting — RateLimiting/PoliticasDeLimite.cs
builder.Services.AddLimitesDeTaxa(builder.Configuration);

// Output caching (em memória, por instância). As políticas ficam nos endpoints (.CacheOutput).
builder.Services.AddOutputCache();

// Idempotência — Idempotencia/*
builder.Services.Configure<IdempotenciaOptions>(builder.Configuration.GetSection(IdempotenciaOptions.Secao));
builder.Services.AddSingleton<IArmazemDeIdempotencia, ArmazemDeIdempotenciaEmMemoria>();

var app = builder.Build();

// Pipeline — A ORDEM IMPORTA:
app.UseExceptionHandler();    // exceção → 500 ProblemDetails (inclusive vinda do handler idempotente)
app.UseStatusCodePages();
app.UseAuthentication();      // 1. quem é? (a partição "por cliente" depende disto)
app.UseAuthorization();       // 2. pode? (401/403 antes de gastar cota)
app.UseRateLimiter();         // 3. dentro da cota? (antes do cache: hit de cache também conta)
app.UseOutputCache();         // 4. já tenho a resposta pronta?
app.UseMiddleware<IdempotenciaMiddleware>(); // 5. já processei esta Idempotency-Key?

app.MapCatalogoEndpoints();
app.MapPedidoEndpoints();
app.MapRelatorioEndpoints();

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
