using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace F4M07.Patterns.Tests;

/// <summary>Monta o container dos testes como a aplicação monta, com relógio falso e logs em memória.</summary>
internal static class Composicao
{
    public static ServiceProvider CriarProvider(
        Action<IServiceCollection>? antes = null,
        FakeTimeProvider? tempo = null,
        Action<IServiceCollection>? depois = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(tempo ?? new FakeTimeProvider(new DateTimeOffset(2026, 11, 27, 12, 0, 0, TimeSpan.Zero)));
        services.AddFakeLogging();
        antes?.Invoke(services);
        services.AddPadroesDoOrderFlow();
        depois?.Invoke(services);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}
