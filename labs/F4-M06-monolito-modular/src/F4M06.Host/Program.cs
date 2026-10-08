using F4M06.Catalogo;
using F4M06.Clientes;
using F4M06.Pedidos;
using F4M06.Shared.Eventos;
using F4M06.Shared.Modulos;

var builder = WebApplication.CreateBuilder(args);

// O Host só COMPÕE: conhece cada módulo pelo IModule e mais nada.
IModule[] modulos = [new CatalogoModule(), new PedidosModule(), new ClientesModule()];

builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddInProcessEventBus();

foreach (var modulo in modulos)
    modulo.Register(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Em desenvolvimento, cria o banco e o schema de cada módulo (catalogo, pedidos, clientes).
// Nos testes, a fixture faz isso; em produção, seriam migrations por módulo no pipeline.
if (app.Environment.IsDevelopment())
    await app.Services.InicializarBancoDosModulosAsync();

foreach (var modulo in modulos)
    modulo.MapEndpoints(app);

await app.RunAsync();

/// <summary>Visível para o WebApplicationFactory&lt;Program&gt; dos testes.</summary>
public partial class Program;
