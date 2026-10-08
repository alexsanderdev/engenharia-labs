using F6M04.Pedidos.Aplicacao;
using F6M04.Pedidos.Infra;
using F6M04.Pedidos.Mensageria;
using F6M04.Pedidos.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace F6M04.Pedidos;

/// <summary>PRONTO. Composição do contexto de Pedidos (o que um Program.cs chamaria).</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registra o DbContext (com o <see cref="OutboxInterceptor"/>), os casos de uso, o publicador RabbitMQ e o
    /// <see cref="OutboxProcessor"/> como hosted service.
    /// </summary>
    /// <param name="configurarDb">Ajustes extras no DbContext (os testes usam para injetar falhas).</param>
    public static IServiceCollection AddPedidos(
        this IServiceCollection services,
        string connectionString,
        Action<RabbitMqOptions>? configurarRabbitMq = null,
        Action<OutboxOptions>? configurarOutbox = null,
        Action<DbContextOptionsBuilder>? configurarDb = null)
    {
        services.AddOptions<RabbitMqOptions>().Configure(o => configurarRabbitMq?.Invoke(o));
        services.AddOptions<OutboxOptions>().Configure(o => configurarOutbox?.Invoke(o));
        services.AddLogging();
        services.TryAddSingleton(TimeProvider.System); // os testes registram um FakeTimeProvider antes

        services.AddSingleton<OutboxInterceptor>();
        services.AddDbContext<PedidosDbContext>((sp, o) =>
        {
            o.UseSqlServer(connectionString).AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
            configurarDb?.Invoke(o);
        });

        services.AddScoped<ServicoDePedidos>();

        services.AddSingleton<PublicadorRabbitMq>();
        services.AddSingleton<IPublicadorDeMensagens>(sp => sp.GetRequiredService<PublicadorRabbitMq>());

        services.AddSingleton<OutboxProcessor>();
        services.AddHostedService(sp => sp.GetRequiredService<OutboxProcessor>());
        return services;
    }
}
