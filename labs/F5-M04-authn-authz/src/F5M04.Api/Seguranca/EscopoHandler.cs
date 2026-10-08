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
        // TODO (Passo 2): junte os valores dos claims TiposAceitos ("scope" e "scp"), separe por espaço e chame
        // context.Succeed(requirement) se algum for EXATAMENTE requirement.Escopo (ordinal, sensível a maiúsculas).
        // Não chame context.Fail(): não satisfazer já nega.
        _ = TiposAceitos;
        throw new NotImplementedException("TODO (Passo 2): verificar o escopo no claim scope/scp (string separada por espaços).");
    }
}
