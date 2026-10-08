using System.Security.Claims;

namespace F5M04.Api.Seguranca;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// O id do cliente é o <c>sub</c> do token, NUNCA um campo do corpo ou da rota.
    /// (No lab, o <c>sub</c> é o próprio id do cliente; num sistema real você mapearia sub → cliente no cadastro.)
    /// </summary>
    public static bool TryObterClienteId(this ClaimsPrincipal user, out Guid clienteId)
    {
        clienteId = Guid.Empty;
        var sub = user.FindFirst(TiposDeClaim.Sub)?.Value;
        return Guid.TryParse(sub, out clienteId) && clienteId != Guid.Empty;
    }
}

/// <summary>O que a API enxerga do token. Útil para depurar o mapeamento de claims.</summary>
public sealed record MeResponse(string? Sub, string? Nome, string[] Papeis, string[] Escopos, string[] TiposDeClaimRecebidos, bool EhAdmin, bool EhCliente);

public static class ContaEndpoints
{
    public static IEndpointRouteBuilder MapContaEndpoints(this IEndpointRouteBuilder app)
    {
        // Exige usuário autenticado explicitamente (DefaultPolicy), independente da FallbackPolicy.
        app.MapGet("/me", (ClaimsPrincipal user) => TypedResults.Ok(new MeResponse(
                Sub: user.FindFirst(TiposDeClaim.Sub)?.Value,
                Nome: user.Identity?.Name,
                Papeis: [.. user.FindAll(TiposDeClaim.Papel).Select(c => c.Value)],
                Escopos: [.. user.FindAll(TiposDeClaim.Escopo).SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))],
                TiposDeClaimRecebidos: [.. user.Claims.Select(c => c.Type).Distinct()],
                EhAdmin: user.IsInRole(Papeis.Admin),
                EhCliente: user.IsInRole(Papeis.Cliente))))
            .RequireAuthorization();
        return app;
    }
}
