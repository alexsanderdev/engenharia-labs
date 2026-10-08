using F3M05.EfAvancado.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F3M05.EfAvancado.Persistencia.Configuracoes;

/// <summary>
/// Mapeamento de <see cref="Produto"/> com dois value objects:
/// <list type="bullet">
/// <item><see cref="Sku"/> → UMA coluna <c>Sku varchar(20)</c> única, via value conversion.</item>
/// <item><see cref="Dinheiro"/> → owned type em DUAS colunas da própria tabela:
/// <c>PrecoValor decimal(18,2)</c> e <c>PrecoMoeda char(3)</c>.</item>
/// </list>
/// </summary>
public sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.Property(p => p.Nome).HasMaxLength(100);

        builder.Property(p => p.Sku)
            .HasConversion(sku => sku.Valor, valor => new Sku(valor))
            .HasMaxLength(20)
            .IsUnicode(false);
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.OwnsOne(p => p.Preco, preco =>
        {
            preco.Property(d => d.Valor).HasColumnName("PrecoValor").HasPrecision(18, 2);
            preco.Property(d => d.Moeda).HasColumnName("PrecoMoeda").HasMaxLength(3).IsFixedLength().IsUnicode(false);
        });
        builder.Navigation(p => p.Preco).IsRequired();
    }
}
