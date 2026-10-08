using F4M01.Api.Erros;
using F4M01.Application.Pedidos;
using F4M01.Infrastructure;

namespace F4M01.Api.Composicao;

/// <summary>
/// Composition root da Api: liga casos de uso (Application) às implementações das portas (Infrastructure).
/// É a ÚNICA parte da Api autorizada a conhecer a Infrastructure.
/// </summary>
public static class ComposicaoDaApi
{
    public static IServiceCollection AdicionarOrderFlow(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("OrderFlow")
            ?? throw new InvalidOperationException("Connection string 'OrderFlow' não configurada.");

        services.AddProblemDetails();
        services.AddExceptionHandler<ErrosDeAplicacaoHandler>();

        services.AddInfrastructure(connectionString);

        services.AddScoped<CriarPedidoHandler>();
        services.AddScoped<ListarPedidosDoClienteHandler>();
        return services;
    }
}
