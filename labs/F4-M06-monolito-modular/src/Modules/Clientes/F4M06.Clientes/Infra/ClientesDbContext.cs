using F4M06.Clientes.Dominio;
using Microsoft.EntityFrameworkCore;

namespace F4M06.Clientes.Infra;

/// <summary>DbContext PRIVADO de Clientes: só enxerga as tabelas do schema <c>clientes</c>.</summary>
internal sealed class ClientesDbContext(DbContextOptions<ClientesDbContext> options) : DbContext(options)
{
    public const string Schema = "clientes";

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<PedidoContabilizado> PedidosContabilizados => Set<PedidoContabilizado>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Cliente>(cliente =>
        {
            cliente.ToTable("Clientes");
            cliente.HasKey(c => c.Id);
            cliente.Property(c => c.Id).ValueGeneratedNever();
            cliente.Property(c => c.Nome).HasMaxLength(200).IsRequired();
            cliente.Property(c => c.Email).HasMaxLength(320).IsRequired();
            cliente.Property(c => c.TotalGasto).HasPrecision(18, 2);
            cliente.Property(c => c.Nivel).HasConversion<string>().HasMaxLength(10);
        });

        modelBuilder.Entity<PedidoContabilizado>(contabilizado =>
        {
            contabilizado.ToTable("PedidosContabilizados");
            contabilizado.HasKey(p => p.PedidoId);
            contabilizado.Property(p => p.PedidoId).ValueGeneratedNever();
            contabilizado.Property(p => p.Valor).HasPrecision(18, 2);
        });
    }
}
