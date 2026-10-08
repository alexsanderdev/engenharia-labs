using F3M04.EfCore.Dominio;
using Microsoft.EntityFrameworkCore;

namespace F3M04.EfCore.Persistencia;

/// <summary>
/// Unit of work do OrderFlow: rastreia as entidades carregadas e grava tudo numa
/// única transação no <c>SaveChanges</c>. Vida curta: um por requisição (scoped).
/// </summary>
public sealed class OrderFlowDbContext(DbContextOptions<OrderFlowDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    /// <summary>
    /// Aplica as classes <c>IEntityTypeConfiguration&lt;T&gt;</c> da pasta Configuracoes.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO (Passo 1): aplique todas as configurações deste assembly
        // (uma linha: modelBuilder.ApplyConfigurationsFromAssembly(...)).
        // Enquanto isso não for feito, o EF usa SÓ as convenções: rode os testes e veja o que elas produzem.
    }
}
