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

        // TODO (Passo 1): configure o JWT Bearer. Use autenticacao.Value, chaves.ChavesDeValidacao e relogio.
        //  - options.MapInboundClaims = false (claims com o nome do JWT: sub, role, scope);
        //  - options.TimeProvider = relogio;
        //  - options.TokenValidationParameters = new TokenValidationParameters { ... } com:
        //      emissor e audiência validados (valores de AutenticacaoOptions);
        //      só tokens assinados, só RS256 (ValidAlgorithms), IssuerSigningKeys = chaves.ChavesDeValidacao;
        //      lifetime validado, exp obrigatório, ClockSkew = ToleranciaDeRelogio;
        //      LifetimeValidator chamando ValidarJanelaDeValidade(..., relogio.GetUtcNow().UtcDateTime);
        //      NameClaimType = "sub" e RoleClaimType = "role" (use TiposDeClaim).
        _ = (autenticacao, chaves, relogio);
        throw new NotImplementedException("TODO (Passo 1): configurar JwtBearerOptions (issuer, audience, assinatura RS256, lifetime, clock skew, claims).");
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);

    /// <summary>
    /// Mesma regra de <c>Validators.ValidateLifetime</c>: exige <c>exp</c>; recusa <c>nbf</c> depois de <c>exp</c>;
    /// aceita só se <c>nbf - tolerância &lt;= agora &lt;= exp + tolerância</c>. Lança as exceções da biblioteca
    /// para o desafio 401 trazer o motivo certo (<c>error_description="The token expired at ..."</c>).
    /// A biblioteca compara com DateTime.UtcNow e não expõe TimeProvider publicamente; por isso o delegate.
    /// </summary>
    internal static bool ValidarJanelaDeValidade(DateTime? notBefore, DateTime? expires, TimeSpan tolerancia, DateTime agoraUtc)
    {
        // TODO (Passo 1): sem exp → SecurityTokenNoExpirationException; nbf > exp → SecurityTokenInvalidLifetimeException;
        // nbf > agora + tolerância → SecurityTokenNotYetValidException; exp < agora - tolerância → SecurityTokenExpiredException;
        // caso contrário, true.
        throw new NotImplementedException("TODO (Passo 1): validar a janela nbf/exp com a tolerância e o relógio injetado.");
    }
}
