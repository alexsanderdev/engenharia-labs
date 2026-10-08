using F6M04.Notificacoes.Inbox;
using F6M04.Notificacoes.Infra;
using F6M04.Notificacoes.Mensageria;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace F6M04.Notificacoes;

/// <summary>PRONTO. Composição do serviço de Notificações.</summary>
public static class DependencyInjection
{
    /// <param name="configurarDb">Ajustes extras no DbContext (os testes usam para injetar falhas).</param>
    public static IServiceCollection AddNotificacoes(
        this IServiceCollection services,
        string connectionString,
        Action<NotificacoesRabbitMqOptions>? configurarRabbitMq = null,
        Action<DbContextOptionsBuilder>? configurarDb = null)
    {
        services.AddOptions<NotificacoesRabbitMqOptions>().Configure(o => configurarRabbitMq?.Invoke(o));
        services.AddLogging();
        services.TryAddSingleton(TimeProvider.System);

        services.AddDbContext<NotificacoesDbContext>(o =>
        {
            o.UseSqlServer(connectionString);
            configurarDb?.Invoke(o);
        });

        services.AddScoped<ConsumidorDeNotificacoes>();

        services.AddSingleton<WorkerDeNotificacoes>();
        services.AddHostedService(sp => sp.GetRequiredService<WorkerDeNotificacoes>());
        return services;
    }
}
