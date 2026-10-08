using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace F2M07.Api.Tests.Infra;

/// <summary>
/// Autenticação fake: o teste diz QUEM é o usuário pelo header <see cref="HeaderClienteId"/>.
/// Sem o header, não autentica (NoResult) e o pipeline devolve 401 normalmente.
/// Assim testamos autorização de verdade sem emitir tokens.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string HeaderClienteId = "X-Test-ClienteId";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
        throw new NotImplementedException(
            "TODO (Passo 4): sem o header X-Test-ClienteId (ou com valor que não é Guid) devolva " +
            "AuthenticateResult.NoResult(); com ele, crie uma ClaimsIdentity (authenticationType = SchemeName) " +
            "com a claim ClaimTypes.NameIdentifier = clienteId e devolva AuthenticateResult.Success(ticket).");
}
