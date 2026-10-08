using F1M07.Api.Infrastructure;
using F1M07.Api.Products;

var builder = WebApplication.CreateBuilder(args);

// Serviços (container de DI)
builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

// OpenAPI nativo (pacote Microsoft.AspNetCore.OpenApi): documento em GET /openapi/v1.json
builder.Services.AddOpenApi();

var app = builder.Build();

// Pipeline de middleware — A ORDEM IMPORTA.
// 1. Correlation id primeiro: assim TODA resposta (inclusive 404 de rota e 500) leva o header.
app.UseMiddleware<CorrelationIdMiddleware>();
// 2. Tratamento de exceções devolve ProblemDetails em vez de stack trace.
app.UseExceptionHandler();
// 3. Status codes sem corpo (ex.: 404 de rota inexistente) também viram ProblemDetails.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.MapProductEndpoints();

app.Run();

/// <summary>
/// Torna a classe Program (gerada pelos top-level statements) visível para
/// o WebApplicationFactory&lt;Program&gt; do projeto de testes.
/// </summary>
public partial class Program;
