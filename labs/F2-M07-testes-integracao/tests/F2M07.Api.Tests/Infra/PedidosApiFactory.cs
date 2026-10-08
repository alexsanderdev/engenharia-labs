using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

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
    private void ApontarParaOContainer(IWebHostBuilder builder) =>
        throw new NotImplementedException(
            $"TODO (Passo 2): use builder.UseSetting(\"ConnectionStrings:Pedidos\", connectionString) (hoje: {connectionString.Length} caracteres) e, em " +
            $"builder.ConfigureTestServices, remova o TimeProvider registrado e registre o relógio recebido ({relogio.GetType().Name}).");

    /// <summary>
    /// Passo 4: registra o esquema <see cref="TestAuthHandler.SchemeName"/> e o torna o esquema padrão.
    /// </summary>
    private static void TrocarAutenticacao(IWebHostBuilder builder)
    {
        // TODO (Passo 4): em builder.ConfigureTestServices, chame
        // services.AddAuthentication(TestAuthHandler.SchemeName).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(...).
        // Enquanto isso não for feito, o header X-Test-ClienteId é ignorado e tudo responde 401.
        _ = builder;
    }
}
