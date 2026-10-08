using F3M04.EfCore.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F3M04.EfCore.Persistencia.Configuracoes;

/// <summary>
/// Mapeamento de <see cref="Pedido"/>: tabela <c>Pedidos</c>; <c>Status</c> gravado como texto
/// (até 20); <c>Total</c> decimal(18,2); FK para <c>Clientes</c> sem cascade (Restrict);
/// índice em <c>ClienteId</c>; um-para-muitos com os itens via FK sombra <c>PedidoId</c>
/// (obrigatória, cascade: item não vive sem pedido).
/// </summary>
public sealed class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        // TODO (Passo 2): tabela "Pedidos"; chave Id gerada pelo domínio; Total com precisão 18,2.
        // TODO (Passo 2): Status convertido para string (HasConversion<string>()) com tamanho 20.
        // TODO (Passo 3): FK ClienteId → Clientes (HasOne<Cliente>().WithMany()...) com Restrict + índice em ClienteId.
        // TODO (Passo 3): HasMany(Itens).WithOne() com FK sombra "PedidoId", obrigatória, Cascade;
        //                 Navigation(Itens) com PropertyAccessMode.Field (o EF escreve no campo _itens).
    }
}
