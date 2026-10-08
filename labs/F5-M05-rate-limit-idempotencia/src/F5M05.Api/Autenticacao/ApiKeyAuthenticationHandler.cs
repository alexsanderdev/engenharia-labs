using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace F5M05.Api.Autenticacao;

// ARQUIVO PRONTO — não precisa alterar. (Autenticação de verdade é o módulo 5.04;
// aqui uma API key simples basta para o rate limit e a idempotência saberem QUEM é o cliente.)

/// <summary>Mapa "API key → id do cliente". Em produção: chaves com hash, vindas do Key Vault.</summary>
public sealed class ApiKeysOptions
{
    public const string Secao = "ApiKeys";
    public Dictionary<string, string> Chaves { get; set; } = [];
}

/// <summary>Autentica pelo header <c>X-Api-Key</c> e emite a claim <c>cliente_id</c>.</summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptionsMonitor<ApiKeysOptions> chaves)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Esquema = "ApiKey";
    public const string Cabecalho = "X-Api-Key";
    public const string ClaimDoCliente = "cliente_id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(Cabecalho, out var valor) || string.IsNullOrWhiteSpace(valor))
            return Task.FromResult(AuthenticateResult.NoResult());

        var recebida = Encoding.UTF8.GetBytes(valor.ToString());
        foreach (var (chave, clienteId) in chaves.CurrentValue.Chaves)
        {
            // Comparação em tempo constante: o tempo de resposta não revela prefixos corretos.
            if (CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(chave), recebida))
            {
                var identidade = new ClaimsIdentity([new Claim(ClaimDoCliente, clienteId)], Esquema);
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidade), Esquema)));
            }
        }

        return Task.FromResult(AuthenticateResult.Fail("API key inválida."));
    }
}

public static class ClaimsPrincipalExtensions
{
    public static string? ClienteId(this ClaimsPrincipal usuario) =>
        usuario.FindFirst(ApiKeyAuthenticationHandler.ClaimDoCliente)?.Value;
}
