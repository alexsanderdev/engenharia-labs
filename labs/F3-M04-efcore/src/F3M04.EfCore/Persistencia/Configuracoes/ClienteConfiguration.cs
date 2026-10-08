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
        // TODO (Passo 2): tabela "Clientes"; chave Id gerada pelo domínio (ValueGeneratedNever).
        // TODO (Passo 2): Nome obrigatório (100), Email obrigatório (200).
        // TODO (Passo 3): índice ÚNICO em Email.
    }
}
