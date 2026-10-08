using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace F5M04.Api.Seguranca;

/// <summary>
/// Configura o esquema JWT Bearer a partir de <see cref="AutenticacaoOptions"/>, das chaves públicas
/// de <see cref="IChavesDeAssinatura"/> e do <see cref="TimeProvider"/> injetado (os testes usam FakeTimeProvider).
/// Registrado com <c>services.ConfigureOptions&lt;ConfigurarJwtBearer&gt;()</c> no Program.cs.
/// </summary>
public sealed class ConfigurarJwtBearer(
    IOptions<AutenticacaoOptions> autenticacao,
    IChavesDeAssinatura chaves,
    TimeProvider relogio) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        // Só o nosso esquema. IConfigureNamedOptions é chamado para TODO nome de JwtBearerOptions.
        if (name != JwtBearerDefaults.AuthenticationScheme) return;

        var opcoes = autenticacao.Value;

        // Mantém os claims com o nome do JWT (sub, role, scope). Sem isso, "sub" vira
        // http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier e "role" vira ClaimTypes.Role.
        options.MapInboundClaims = false;
        options.TimeProvider = relogio;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Quem emitiu? Só aceitamos o NOSSO emissor.
            ValidateIssuer = true,
            ValidIssuer = opcoes.Emissor,

            // Para quem foi emitido? Token para outra API (ou um id_token do front) é recusado.
            ValidateAudience = true,
            ValidAudience = opcoes.Audiencia,

            // Assinatura: só tokens assinados, só RS256, só com as nossas chaves. Fecha "alg: none" e confusão de algoritmo.
            RequireSignedTokens = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            IssuerSigningKeys = chaves.ChavesDeValidacao,
            ValidateIssuerSigningKey = true,

            // Tempo: exp obrigatório e tolerância curta (o padrão da biblioteca é 5 min).
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = opcoes.ToleranciaDeRelogio,
            // A biblioteca compara com DateTime.UtcNow e não expõe TimeProvider publicamente.
            // Este delegate SUBSTITUI a validação de tempo padrão pela mesma regra, lendo o relógio injetado.
            LifetimeValidator = (notBefore, expires, _, parametros) =>
                ValidarJanelaDeValidade(notBefore, expires, parametros.ClockSkew, relogio.GetUtcNow().UtcDateTime),

            // Mapeamento EXPLÍCITO: User.Identity.Name = sub; User.IsInRole("Admin") lê o claim "role".
            NameClaimType = TiposDeClaim.Sub,
            RoleClaimType = TiposDeClaim.Papel,
        };
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);

    /// <summary>
    /// Mesma regra de <c>Validators.ValidateLifetime</c>: exige <c>exp</c>; recusa <c>nbf</c> depois de <c>exp</c>;
    /// aceita só se <c>nbf - tolerância &lt;= agora &lt;= exp + tolerância</c>. Lança as exceções da biblioteca
    /// para o desafio 401 trazer o motivo certo (<c>error_description="The token expired at ..."</c>).
    /// </summary>
    internal static bool ValidarJanelaDeValidade(DateTime? notBefore, DateTime? expires, TimeSpan tolerancia, DateTime agoraUtc)
    {
        if (expires is not { } exp)
            throw new SecurityTokenNoExpirationException("Token sem 'exp'.");

        if (notBefore is { } nbf && nbf > exp)
            throw new SecurityTokenInvalidLifetimeException("'nbf' posterior a 'exp'.") { NotBefore = nbf, Expires = exp };

        if (notBefore is { } inicio && inicio > agoraUtc + tolerancia)
            throw new SecurityTokenNotYetValidException($"Token ainda não é válido (nbf {inicio:O}).") { NotBefore = inicio };

        if (exp < agoraUtc - tolerancia)
            throw new SecurityTokenExpiredException($"Token expirado em {exp:O}.") { Expires = exp };

        return true;
    }
}
