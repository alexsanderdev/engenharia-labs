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

        // TODO (Passo 2): troque os dois Ignore abaixo pelo mapeamento de verdade.
        //  - Sku: HasConversion(sku => sku.Valor, valor => new Sku(valor)), HasMaxLength(20), IsUnicode(false)
        //         + índice único em Sku.
        //  - Preco: OwnsOne(p => p.Preco, preco => { colunas "PrecoValor" (18,2) e "PrecoMoeda" (3, fixo, não unicode) })
        //           + Navigation(p => p.Preco).IsRequired().
        // Enquanto estiverem ignorados, o EF simplesmente NÃO grava nem lê esses valores.
        builder.Ignore(p => p.Sku);
        builder.Ignore(p => p.Preco);
    }
}
