using F3M04.EfCore.Dominio;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace F3M04.EfCore.Persistencia.Configuracoes;

/// <summary>
/// Mapeamento de <see cref="Cliente"/>: tabela <c>Clientes</c>; <c>Nome</c> obrigatório até 100;
/// <c>Email</c> obrigatório, até 200 e único.
/// </summary>
public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Nome).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200).IsRequired();

        builder.HasIndex(c => c.Email).IsUnique();
    }
}
