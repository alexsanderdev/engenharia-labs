using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using F5M04.Api.Seguranca;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace F5M04.Api.Tests.Infra;

/// <summary>Descrição de um token de teste. Use <c>with</c> para variar um campo de cada vez.</summary>
public sealed record EspecificacaoDeToken
{
    public required string Sub { get; init; }
    public IReadOnlyList<string> Papeis { get; init; } = [];
    /// <summary>Escopos separados por espaço, como no claim <c>scope</c> real. Vazio = sem claim.</summary>
    public string Escopos { get; init; } = "";
    public string Emissor { get; init; } = EmissorDeTokensDeTeste.Emissor;
    public string Audiencia { get; init; } = EmissorDeTokensDeTeste.Audiencia;
    /// <summary>Deslocamento de <c>iat</c>/<c>nbf</c> em relação ao "agora" do relógio falso.</summary>
    public TimeSpan EmitidoHa { get; init; } = TimeSpan.Zero;
    public TimeSpan Validade { get; init; } = EmissorDeTokensDeTeste.ValidadePadrao;
    /// <summary>Se false, o token sai sem <c>exp</c>.</summary>
    public bool ComExpiracao { get; init; } = true;
}

/// <summary>
/// O "provedor de identidade" dos testes: gera um par RSA EM MEMÓRIA (nada em disco, nada de segredo versionado),
/// assina tokens RS256 com a chave privada e entrega à API só a chave PÚBLICA (via <see cref="IChavesDeAssinatura"/>),
/// exatamente como o Entra ID faz com o JWKS. O tempo vem do <see cref="FakeTimeProvider"/> compartilhado com a API.
/// </summary>
public sealed class EmissorDeTokensDeTeste(FakeTimeProvider relogio) : IChavesDeAssinatura
{
    public const string Emissor = "https://login.teste.orderflow.local/";
    public const string Audiencia = "api://orderflow-teste";
    public static readonly TimeSpan ValidadePadrao = TimeSpan.FromMinutes(5);

    // Um par novo por emissor (= por API de teste). Nunca sai da memória.
    // Não compartilhe a MESMA instância de chave entre hosts que rodam em paralelo: neste lab isso causou
    // falhas intermitentes de assinatura (suspeita: cache estático de SignatureProvider do Microsoft.IdentityModel).
    private readonly RsaSecurityKey _chavePrivada = NovaChave("teste");
    private readonly Lazy<RsaSecurityKey> _chaveIntrusa = new(() => NovaChave("intrusa"));

    private static readonly JsonWebTokenHandler Handler = new() { SetDefaultTimesOnTokenCreation = false };

    /// <summary>Só a parte PÚBLICA da chave (é o que um JWKS publica).</summary>
    public IReadOnlyCollection<SecurityKey> ChavesDeValidacao => field ??=
        [new RsaSecurityKey(_chavePrivada.Rsa.ExportParameters(includePrivateParameters: false)) { KeyId = _chavePrivada.KeyId }];

    public FakeTimeProvider Relogio { get; } = relogio;

    /// <summary>Token RS256 assinado pela chave confiável.</summary>
    public string Emitir(EspecificacaoDeToken spec) => Assinar(spec, _chavePrivada);

    /// <summary>Token com formato perfeito, mas assinado por uma chave que a API não conhece.</summary>
    public string EmitirComOutraChave(EspecificacaoDeToken spec) => Assinar(spec, _chaveIntrusa.Value);

    /// <summary>Token "alg: none": cabeçalho e payload válidos, assinatura vazia. Clássico ataque a validações ingênuas.</summary>
    public string EmitirSemAssinatura(EspecificacaoDeToken spec)
    {
        var cabecalho = Base64UrlEncoder.Encode("""{"alg":"none","typ":"JWT"}""");
        var payload = Base64UrlEncoder.Encode(JsonSerializer.Serialize(Payload(spec)));
        return $"{cabecalho}.{payload}.";
    }

    /// <summary>Pega um token válido e troca o payload (ex.: promove Cliente a Admin) mantendo a assinatura original.</summary>
    public static string Adulterar(string tokenValido, Action<JsonObject> alterar)
    {
        var partes = tokenValido.Split('.');
        var payload = JsonNode.Parse(Base64UrlEncoder.Decode(partes[1]))!.AsObject();
        alterar(payload);
        partes[1] = Base64UrlEncoder.Encode(payload.ToJsonString());
        return string.Join('.', partes);
    }

    private string Assinar(EspecificacaoDeToken spec, RsaSecurityKey chave)
    {
        var agora = Relogio.GetUtcNow().UtcDateTime;
        var emitidoEm = agora - spec.EmitidoHa;
        var claims = new Dictionary<string, object> { [TiposDeClaim.Sub] = spec.Sub };
        if (spec.Papeis.Count > 0) claims[TiposDeClaim.Papel] = spec.Papeis.ToArray();
        if (!string.IsNullOrWhiteSpace(spec.Escopos)) claims[TiposDeClaim.Escopo] = spec.Escopos;

        return Handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = spec.Emissor,
            Audience = spec.Audiencia,
            IssuedAt = emitidoEm,
            NotBefore = emitidoEm,
            Expires = spec.ComExpiracao ? emitidoEm + spec.Validade : null,
            Claims = claims,
            SigningCredentials = new SigningCredentials(chave, SecurityAlgorithms.RsaSha256),
        });
    }

    private Dictionary<string, object> Payload(EspecificacaoDeToken spec)
    {
        var agora = Relogio.GetUtcNow();
        return new()
        {
            ["iss"] = spec.Emissor,
            ["aud"] = spec.Audiencia,
            [TiposDeClaim.Sub] = spec.Sub,
            [TiposDeClaim.Papel] = spec.Papeis,
            ["iat"] = agora.ToUnixTimeSeconds(),
            ["nbf"] = agora.ToUnixTimeSeconds(),
            ["exp"] = (agora + spec.Validade).ToUnixTimeSeconds(),
        };
    }

    private static RsaSecurityKey NovaChave(string prefixo) => new(RSA.Create(2048)) { KeyId = $"{prefixo}-{Guid.NewGuid():N}" };

    /// <summary>Decodifica o payload (só para inspeção nos testes; isso NÃO valida nada).</summary>
    public static JsonObject LerPayload(string token) =>
        JsonNode.Parse(Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(token.Split('.')[1])))!.AsObject();
}
