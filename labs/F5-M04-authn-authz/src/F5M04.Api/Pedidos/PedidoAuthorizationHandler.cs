using F5M04.Api.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace F5M04.Api.Pedidos;

/// <summary>
/// Autorização baseada em RECURSO: a decisão depende do pedido carregado (quem é o dono),
/// não só do token. É a defesa contra BOLA/IDOR (OWASP API1:2023).
/// Regras: o dono lê e cancela o próprio pedido; Admin lê qualquer pedido, mas não cancela pedido alheio;
/// qualquer outra combinação (ou operação desconhecida) é negada.
/// </summary>
public sealed class PedidoAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Pedido>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Pedido resource)
    {
        // TODO (Passo 3): ehDono = context.User.TryObterClienteId(out var id) && id == resource.ClienteId;
        // ehAdmin = context.User.IsInRole(Papeis.Admin). Ler: dono ou Admin. Cancelar: só o dono.
        // Qualquer outra operação: negar. Para permitir, context.Succeed(requirement); para negar, não faça nada.
        throw new NotImplementedException("TODO (Passo 3): decidir Ler/Cancelar pelo dono do pedido e pelo papel Admin.");
    }
}
