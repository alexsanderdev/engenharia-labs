using F4M01.Domain.Pedidos;
using F4M01.Domain.Produtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F4M01.Infrastructure.Persistencia;

// Mapeamento com Fluent API: é AQUI que o domínio vira tabela.

internal sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        // TODO (Passo 2): a precisão do Total está num atributo [Precision] DENTRO do domínio.
        // Mova para cá: builder.Property(p => p.Total).HasPrecision(18, 2);
        builder.HasIndex(p => p.ClienteId);

        // Itens fazem parte do agregado: owned collection, gravada em tabela própria.
        builder.OwnsMany(p => p.Itens, item =>
        {
            item.ToTable("ItensPedido");
            item.WithOwner().HasForeignKey("PedidoId");
            item.Property<int>("Id");
            item.HasKey("Id");
            item.Property(i => i.NomeProduto).HasMaxLength(100);
            item.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
            item.Ignore(i => i.Subtotal);
        });
        // TODO (Passo 2): quando Itens virar IReadOnlyList com campo _itens, acrescente:
        // builder.Navigation(p => p.Itens).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ProdutoConfiguration : IEntityTypeConfiguration<Produto>
{
    public void Configure(EntityTypeBuilder<Produto> builder)
    {
        builder.ToTable("Produtos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Nome).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Preco).HasPrecision(18, 2);
    }
}
