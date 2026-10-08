using F4M06.Catalogo.Dominio;
using Microsoft.EntityFrameworkCore;

namespace F4M06.Catalogo.Infra;

/// <summary>DbContext PRIVADO do Catálogo: só enxerga as tabelas do schema <c>catalogo</c>.</summary>
internal sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    public const string Schema = "catalogo";

    public DbSet<Produto> Produtos => Set<Produto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Produto>(produto =>
        {
            produto.ToTable("Produtos");
            produto.HasKey(p => p.Id);
            produto.Property(p => p.Id).ValueGeneratedNever();
            produto.Property(p => p.Nome).HasMaxLength(200).IsRequired();
            produto.Property(p => p.Preco).HasPrecision(18, 2);
        });
    }
}
