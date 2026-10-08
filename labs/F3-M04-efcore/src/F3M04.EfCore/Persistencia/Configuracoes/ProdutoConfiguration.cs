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
        // TODO (Passo 2): tabela "Produtos"; chave Id gerada pelo domínio (ValueGeneratedNever).
        // TODO (Passo 2): Sku obrigatório (30), Nome obrigatório (100), Preco com precisão 18,2.
        // TODO (Passo 3): índice ÚNICO em Sku.
        // TODO (Passo 4): seed com HasData(CatalogoInicial.Produtos).
    }
}
