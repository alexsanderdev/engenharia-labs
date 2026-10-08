using System.Text.Json.Serialization;
using F5M02.Api.Configuracao;
using F5M02.Api.Dominio;
using F5M02.Api.Erros;
using F5M02.Api.Http;
using F5M02.Api.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Os defaults "web" do System.Text.Json aceitam números como string ("620.00") — e o OpenAPI gerado documenta
// isso fielmente ("type": ["number","string"]). Strict deixa o contrato honesto: número é número.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);

// Passo 1 — erros padronizados (RFC 9457) em TODA a API.
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsPadrao.Customizar);

// Passo 2 — versionamento (Configuracao/Versionamento.cs).
builder.Services.AddVersionamentoDaApi();

// Passo 4 — documentos OpenAPI por versão (OpenApi/DocumentacaoOpenApi.cs).
builder.Services.AddDocumentacaoOpenApi();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PedidoRepositorio>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapDocumentacaoOpenApi();

// Passo 3 — endpoints v1 (depreciada) e v2 (Http/PedidosEndpoints.cs).
app.MapPedidosVersionados();

// Endpoint de diagnóstico do LAB: simula um bug com dado sensível na mensagem. Fora do versionamento e da documentação.
app.MapGet("/diagnostico/falha", IResult () =>
        throw new InvalidOperationException("Falha ao abrir conexão: Server=sql-prod;User Id=sa;Password=SenhaSecreta123"))
    .ExcludeFromDescription();

app.Run();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
