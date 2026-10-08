using F4M01.Api.Composicao;
using F4M01.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Composition root: o ÚNICO lugar que conhece todas as peças (inclusive a Infrastructure).
builder.Services.AdicionarOrderFlow(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapPedidoEndpoints();

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
