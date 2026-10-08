using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace F5M04.Api.Seguranca;

/// <summary>
/// De onde vêm as chaves PÚBLICAS que validam a assinatura dos tokens.
/// Em produção com Entra ID você não precisa disto: <c>Authority</c> baixa o JWKS do emissor
/// (<c>/.well-known/openid-configuration</c> → <c>jwks_uri</c>) e faz rotação de chaves sozinho.
/// No lab, os testes registram a chave do emissor de teste; em Development, <see cref="ChavesDeDesenvolvimento"/>.
/// </summary>
public interface IChavesDeAssinatura
{
    IReadOnlyCollection<SecurityKey> ChavesDeValidacao { get; }
}

/// <summary>
/// Padrão fora de Development enquanto nenhum provedor real (Entra ID) estiver configurado:
/// nenhuma chave, logo nenhum token é aceito (falha FECHADA). O catálogo anônimo continua funcionando.
/// </summary>
public sealed class SemChavesDeAssinatura : IChavesDeAssinatura
{
    public IReadOnlyCollection<SecurityKey> ChavesDeValidacao => [];
}

/// <summary>
/// SOMENTE Development: par RSA efêmero (gerado em memória a cada execução, nunca gravado em disco)
/// para você testar a API com o arquivo <c>.http</c>. Não existe fora de Development (há teste garantindo).
/// Em um time real, prefira <c>dotnet user-jwts</c> ou um IdP de desenvolvimento.
/// </summary>
public sealed class ChavesDeDesenvolvimento : IChavesDeAssinatura, IDisposable
{
    private static readonly TimeSpan Validade = TimeSpan.FromMinutes(15);

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly RsaSecurityKey _chavePrivada;
    private readonly IOptions<AutenticacaoOptions> _opcoes;
    private readonly TimeProvider _relogio;

    public ChavesDeDesenvolvimento(IOptions<AutenticacaoOptions> opcoes, TimeProvider relogio)
    {
        _opcoes = opcoes;
        _relogio = relogio;
        var kid = $"dev-{Guid.NewGuid():N}";
        _chavePrivada = new RsaSecurityKey(_rsa) { KeyId = kid };
        ChavesDeValidacao = [new RsaSecurityKey(_rsa.ExportParameters(includePrivateParameters: false)) { KeyId = kid }];
    }

    public IReadOnlyCollection<SecurityKey> ChavesDeValidacao { get; }

    public (string Token, int ExpiraEmSegundos) Emitir(string sub, IReadOnlyCollection<string> papeis, string? escopos)
    {
        var agora = _relogio.GetUtcNow().UtcDateTime;
        var claims = new Dictionary<string, object>
        {
            [TiposDeClaim.Sub] = sub,
            [TiposDeClaim.Papel] = papeis.ToArray(),
        };
        if (!string.IsNullOrWhiteSpace(escopos)) claims[TiposDeClaim.Escopo] = escopos;

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = _opcoes.Value.Emissor,
            Audience = _opcoes.Value.Audiencia,
            IssuedAt = agora,
            NotBefore = agora,
            Expires = agora + Validade,
            Claims = claims,
            SigningCredentials = new SigningCredentials(_chavePrivada, SecurityAlgorithms.RsaSha256),
        });
        return (token, (int)Validade.TotalSeconds);
    }

    public void Dispose() => _rsa.Dispose();
}

/// <summary>Corpo do <c>POST /dev/token</c>.</summary>
public sealed record PedidoDeTokenDev(string Sub, string[]? Papeis, string? Escopos);

public static class DevTokenEndpoints
{
    /// <summary>Emissor local de tokens. Só é mapeado quando <c>IsDevelopment()</c>.</summary>
    public static IEndpointRouteBuilder MapEmissorDeDesenvolvimento(this IEndpointRouteBuilder app)
    {
        app.MapPost("/dev/token", (PedidoDeTokenDev pedido, ChavesDeDesenvolvimento chaves) =>
            {
                var (token, expiraEm) = chaves.Emitir(pedido.Sub, pedido.Papeis ?? [], pedido.Escopos);
                return TypedResults.Ok(new { access_token = token, token_type = "Bearer", expires_in = expiraEm });
            })
            .AllowAnonymous();
        return app;
    }
}
