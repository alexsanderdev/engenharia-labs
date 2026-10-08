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
        // TODO (Passo 2): registre (este método roda na inicialização, por isso não lança NotImplementedException):
        //  - FallbackPolicy: usuário autenticado (endpoint sem metadado de autorização exige login);
        //  - Politicas.Admin: autenticado + papel Papeis.Admin;
        //  - Politicas.Cliente: autenticado + papel Papeis.Cliente + claim "sub";
        //  - Politicas.PedidosEscrita: autenticado + new EscopoRequirement(Escopos.PedidosEscrita).
        // Sem as políticas nomeadas, endpoints que as usam devolvem 500 ("The AuthorizationPolicy named ... was not found").
        return builder;
    }
}
