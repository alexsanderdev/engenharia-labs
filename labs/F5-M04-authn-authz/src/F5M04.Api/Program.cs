using F5M04.Api.Catalogo;
using F5M04.Api.Pedidos;
using F5M04.Api.Seguranca;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ICatalogo, CatalogoEmMemoria>();
builder.Services.AddSingleton<IPedidoRepositorio, PedidoRepositorioEmMemoria>();

// ---------- Autenticação: QUEM é você? ----------
builder.Services.AddOptions<AutenticacaoOptions>()
    .BindConfiguration(AutenticacaoOptions.Secao)
    .ValidateDataAnnotations()
    .ValidateOnStart();

if (builder.Environment.IsDevelopment())
{
    // Chave efêmera + POST /dev/token para testar com o arquivo .http. Nunca fora de Development.
    builder.Services.AddSingleton<ChavesDeDesenvolvimento>();
    builder.Services.AddSingleton<IChavesDeAssinatura>(sp => sp.GetRequiredService<ChavesDeDesenvolvimento>());
}
// Fora de Development (e sem Entra ID configurado) não há chave: nenhum token é aceito (falha fechada).
builder.Services.TryAddSingleton<IChavesDeAssinatura, SemChavesDeAssinatura>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.ConfigureOptions<ConfigurarJwtBearer>();

// ---------- Autorização: o que você PODE fazer? ----------
builder.Services.AddAuthorizationBuilder()
    .AdicionarPoliticasOrderFlow();
builder.Services.AddSingleton<IAuthorizationHandler, EscopoHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PedidoAuthorizationHandler>();

var app = builder.Build();

// Ordem importa: erros e status sem corpo (401/403/404) viram ProblemDetails;
// autenticação antes de autorização; ambas antes dos endpoints.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapContaEndpoints();
app.MapProdutoEndpoints();
app.MapPedidoEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapEmissorDeDesenvolvimento();
}

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
