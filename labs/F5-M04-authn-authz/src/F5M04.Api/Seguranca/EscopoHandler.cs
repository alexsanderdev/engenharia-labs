using Microsoft.AspNetCore.Authorization;

namespace F5M04.Api.Seguranca;

/// <summary>Requisito: o token precisa conter o escopo <paramref name="Escopo"/>.</summary>
public sealed record EscopoRequirement(string Escopo) : IAuthorizationRequirement;

/// <summary>
/// Avalia <see cref="EscopoRequirement"/>. Escopos chegam como UMA string separada por espaços
/// no claim <c>scope</c> (OAuth 2.0) ou <c>scp</c> (Entra ID): <c>"pedidos.read pedidos.write"</c>.
/// </summary>
public sealed class EscopoHandler : AuthorizationHandler<EscopoRequirement>
{
    private static readonly string[] TiposAceitos = [TiposDeClaim.Escopo, TiposDeClaim.EscopoEntra];

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EscopoRequirement requirement)
    {
        var escopos = context.User.Claims
            .Where(c => TiposAceitos.Contains(c.Type, StringComparer.Ordinal))
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        // Comparação exata e sensível a maiúsculas (escopos são case-sensitive, RFC 6749 §3.3).
        // Nunca use string.Contains no valor bruto: "pedidos.writeall" conteria "pedidos.write".
        if (escopos.Contains(requirement.Escopo, StringComparer.Ordinal))
            context.Succeed(requirement);

        // Sem Fail(): não satisfazer já nega. Fail() impediria qualquer outro handler de aprovar.
        return Task.CompletedTask;
    }
}
