using F3M04.EfCore.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F3M04.EfCore.Persistencia.Configuracoes;

/// <summary>
/// Mapeamento de <see cref="Produto"/>: tabela <c>Produtos</c>; <c>Sku</c> obrigatório,
/// até 30 caracteres e único; <c>Nome</c> obrigatório, até 100; <c>Preco</c> decimal(18,2);
/// seed do <see cref="CatalogoInicial"/> com <c>HasData</c>.
/// </summary>
public sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Sku).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Nome).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Preco).HasPrecision(18, 2);

        builder.HasIndex(p => p.Sku).IsUnique();

        builder.HasData(CatalogoInicial.Produtos);
    }
}
