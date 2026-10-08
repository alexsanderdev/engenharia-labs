using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace F2M07.Api.Infrastructure;

/// <summary>
/// Autenticação "de produção" do lab: header <c>X-Api-Key</c> mapeado para um cliente
/// na seção <c>ApiKeys</c> da configuração. Num sistema real seria JWT/OIDC — e nos testes
/// trocamos este esquema inteiro por um handler fake (ver o projeto de testes).
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";
    public const string Header = "X-Api-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Header, out var chave) || string.IsNullOrWhiteSpace(chave))
            return Task.FromResult(AuthenticateResult.NoResult());

        var clienteId = configuration[$"ApiKeys:{chave}"];
        if (clienteId is null)
            return Task.FromResult(AuthenticateResult.Fail("API key inválida."));

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, clienteId)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id do cliente autenticado (claim NameIdentifier).</summary>
    public static Guid ObterClienteId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? throw new InvalidOperationException("Usuário sem claim NameIdentifier."));
}
