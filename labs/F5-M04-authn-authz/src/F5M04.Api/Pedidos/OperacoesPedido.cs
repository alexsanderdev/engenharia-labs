using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace F5M04.Api.Pedidos;

/// <summary>
/// Operações sobre um <see cref="Pedido"/> usadas na autorização baseada em recurso:
/// <c>authorizationService.AuthorizeAsync(User, pedido, OperacoesPedido.Ler)</c>.
/// </summary>
public static class OperacoesPedido
{
    public const string NomeLer = "Ler";
    public const string NomeCancelar = "Cancelar";

    public static readonly OperationAuthorizationRequirement Ler = new() { Name = NomeLer };
    public static readonly OperationAuthorizationRequirement Cancelar = new() { Name = NomeCancelar };
}
