using F2M07.Api.Infrastructure;
using F2M07.Api.Pedidos;
using F2M07.Api.Produtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

// A connection string é lida de IConfiguration NA HORA de montar as options do DbContext
// (lambda com IServiceProvider), e não no momento do AddDbContext. Isso permite que o
// WebApplicationFactory dos testes sobrescreva "ConnectionStrings:Pedidos" com a do container.
builder.Services.AddDbContext<PedidosDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("Pedidos")));

builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapProdutoEndpoints();
app.MapPedidoEndpoints();

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
