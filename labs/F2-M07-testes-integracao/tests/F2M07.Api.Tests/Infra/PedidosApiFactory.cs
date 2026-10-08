using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace F2M07.Api.Tests.Infra;

/// <summary>
/// WebApplicationFactory customizada: a API real (Program.cs inteiro), com três trocas pontuais:
/// connection string do container, relógio controlado e autenticação fake.
/// </summary>
public sealed class PedidosApiFactory(string connectionString, TimeProvider relogio)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        ApontarParaOContainer(builder); // Passo 2
        TrocarAutenticacao(builder);    // Passo 4
    }

    /// <summary>
    /// Passo 2: sobrescreve "ConnectionStrings:Pedidos" com a do container e troca o
    /// <see cref="TimeProvider"/> registrado no Program.cs pelo relógio controlado da fixture.
    /// </summary>
    private void ApontarParaOContainer(IWebHostBuilder builder)
    {
        // UseSetting entra na configuração com prioridade sobre appsettings.json.
        builder.UseSetting("ConnectionStrings:Pedidos", connectionString);

        // ConfigureTestServices roda DEPOIS dos registros do Program.cs: é o lugar de substituir serviços.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(relogio);
        });
    }

    /// <summary>
    /// Passo 4: registra o esquema <see cref="TestAuthHandler.SchemeName"/> e o torna o esquema padrão.
    /// </summary>
    private static void TrocarAutenticacao(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Novo esquema "Test" passa a ser o padrão; o ApiKey continua registrado, mas não é usado.
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
        });
    }
}
