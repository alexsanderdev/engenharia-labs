using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace F3M07.Dados.Persistencia;

/// <summary>
/// Usada SÓ pelas ferramentas de design time (<c>dotnet ef migrations add / script / bundle</c>).
/// Esta class library não tem Program.cs nem DI; sem esta factory o <c>dotnet ef</c> não saberia
/// como criar o <see cref="LojaDbContext"/>.
/// <para>
/// "migrations add" e "migrations script" NÃO conectam no banco: a connection string só importa para
/// "database update". Para apontar para um banco real, defina a variável de ambiente <c>F3M07_CONNECTION</c>.
/// </para>
/// </summary>
public sealed class LojaDbContextFactory : IDesignTimeDbContextFactory<LojaDbContext>
{
    private const string ConnectionStringPadrao =
        "Server=(localdb)\\MSSQLLocalDB;Database=F3M07Loja;Integrated Security=true;TrustServerCertificate=true";

    public LojaDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("F3M07_CONNECTION") ?? ConnectionStringPadrao;
        var options = new DbContextOptionsBuilder<LojaDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new LojaDbContext(options);
    }
}
