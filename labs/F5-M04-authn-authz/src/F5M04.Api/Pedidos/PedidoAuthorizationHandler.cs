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
        var ehDono = context.User.TryObterClienteId(out var clienteId) && clienteId == resource.ClienteId;
        var ehAdmin = context.User.IsInRole(Papeis.Admin);

        var permitido = requirement.Name switch
        {
            OperacoesPedido.NomeLer => ehDono || ehAdmin,
            OperacoesPedido.NomeCancelar => ehDono,
            _ => false, // negação por padrão: operação nova não ganha acesso "sem querer"
        };

        if (permitido) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
