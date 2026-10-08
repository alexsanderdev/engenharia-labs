using Microsoft.AspNetCore.Authorization;

namespace F5M04.Api.Seguranca;

public static class AutorizacaoExtensions
{
    /// <summary>
    /// Registra as políticas do OrderFlow e a FallbackPolicy.
    /// Chamado no Program.cs: <c>builder.Services.AddAuthorizationBuilder().AdicionarPoliticasOrderFlow()</c>.
    /// </summary>
    public static AuthorizationBuilder AdicionarPoliticasOrderFlow(this AuthorizationBuilder builder)
    {
        // Seguro por padrão: endpoint SEM metadado de autorização exige usuário autenticado.
        // Endpoint público precisa dizer isso explicitamente com AllowAnonymous().
        builder.SetFallbackPolicy(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

        builder.AddPolicy(Politicas.Admin, politica => politica
            .RequireAuthenticatedUser()
            .RequireRole(Papeis.Admin));

        builder.AddPolicy(Politicas.Cliente, politica => politica
            .RequireAuthenticatedUser()
            .RequireRole(Papeis.Cliente)
            .RequireClaim(TiposDeClaim.Sub));

        builder.AddPolicy(Politicas.PedidosEscrita, politica => politica
            .RequireAuthenticatedUser()
            .AddRequirements(new EscopoRequirement(Escopos.PedidosEscrita)));

        return builder;
    }
}
