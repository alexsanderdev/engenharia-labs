using F4M06.Clientes.Contracts;
using F4M06.Clientes.Infra;
using Microsoft.EntityFrameworkCore;

namespace F4M06.Clientes.Fachada;

/// <summary>Implementação INTERNA do contrato público <see cref="IClientesApi"/>.</summary>
internal sealed class ClientesApi(ClientesDbContext db) : IClientesApi
{
    public Task<bool> ExisteAsync(Guid clienteId, CancellationToken ct = default) =>
        db.Clientes.AnyAsync(c => c.Id == clienteId, ct);
}
