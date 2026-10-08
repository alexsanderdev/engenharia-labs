using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace F4M06.Integracao.Tests.Infra;

/// <summary>
/// JÁ VEM PRONTA. O Host real (Program.cs inteiro, com os três módulos) apontando para o banco do container.
/// </summary>
public sealed class OrderFlowApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:OrderFlow", connectionString);
        builder.UseSetting("Logging:LogLevel:Default", "Warning");
    }
}
