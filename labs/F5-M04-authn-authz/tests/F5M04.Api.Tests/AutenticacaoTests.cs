using System.Net;
using F5M04.Api.Seguranca;
using F5M04.Api.Tests.Infra;

namespace F5M04.Api.Tests;

/// <summary>
/// Passo 1 — Autenticação JWT Bearer: QUEM é você? Tudo pelo endpoint <c>GET /me</c> (exige usuário autenticado).
/// Regra de ouro: qualquer falha de validação do token → 401 com <c>WWW-Authenticate: Bearer error="invalid_token"</c>.
/// </summary>
public sealed class AutenticacaoTests : IAsyncDisposable
{
    private readonly ApiFactory _api = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public ValueTask DisposeAsync() => _api.DisposeAsync();

    [Fact]
    public async Task Me_SemToken_Retorna401ComDesafioBearerSemErro()
    {
        var response = await _api.Anonimo().GetAsync("/me", Ct);

        var desafio = await response.DeveSer401ComDesafioBearerAsync();
        // RFC 6750 §3.1: sem credencial, o desafio NÃO deve trazer código de erro.
        desafio.ShouldNotContain("error=");
    }

    [Fact]
    public async Task Me_TokenValido_Retorna200ComClaimsOriginaisDoJwt()
    {
        var response = await _api.ComoCliente(ApiFactory.Ana).GetAsync("/me", Ct);

        var me = await response.LerAsync<MeResponse>(HttpStatusCode.OK);
        me.Sub.ShouldBe(ApiFactory.Ana.ToString());
        me.Nome.ShouldBe(ApiFactory.Ana.ToString(), "NameClaimType deve ser 'sub'.");
        me.EhCliente.ShouldBeTrue("RoleClaimType deve ser 'role' para IsInRole funcionar.");
        me.EhAdmin.ShouldBeFalse();
        me.Escopos.ShouldBe([Escopos.PedidosLeitura, Escopos.PedidosEscrita]);
        me.TiposDeClaimRecebidos.ShouldContain("sub");
        me.TiposDeClaimRecebidos.ShouldContain("role");
        me.TiposDeClaimRecebidos.ShouldNotContain("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            "MapInboundClaims = false: os claims devem chegar com o nome do JWT.");
        me.TiposDeClaimRecebidos.ShouldNotContain("http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
    }

    public static TheoryData<string> TokensInvalidos =>
    [
        "assinado-por-chave-desconhecida",
        "outro-emissor",
        "outra-audiencia",
        "alg-none",
        "payload-adulterado",
        "sem-exp",
        "nbf-no-futuro",
    ];

    [Theory]
    [MemberData(nameof(TokensInvalidos))]
    public async Task Me_TokenInvalido_Retorna401InvalidToken(string caso)
    {
        var cliente = ApiFactory.TokenDeCliente(ApiFactory.Ana);
        var token = caso switch
        {
            "assinado-por-chave-desconhecida" => _api.Emissor.EmitirComOutraChave(cliente),
            "outro-emissor" => _api.Emissor.Emitir(cliente with { Emissor = "https://emissor-malicioso.example/" }),
            "outra-audiencia" => _api.Emissor.Emitir(cliente with { Audiencia = "api://outra-api" }),
            "alg-none" => _api.Emissor.EmitirSemAssinatura(cliente with { Papeis = [Papeis.Admin] }),
            "payload-adulterado" => EmissorDeTokensDeTeste.Adulterar(_api.Emissor.Emitir(cliente),
                payload => payload["role"] = Papeis.Admin),
            "sem-exp" => _api.Emissor.Emitir(cliente with { ComExpiracao = false }),
            "nbf-no-futuro" => _api.Emissor.Emitir(cliente with { EmitidoHa = TimeSpan.FromMinutes(-2) }),
            _ => throw new ArgumentOutOfRangeException(nameof(caso)),
        };

        var response = await _api.ComToken(token).GetAsync("/me", Ct);

        var desafio = await response.DeveSer401ComDesafioBearerAsync();
        desafio.ShouldContain("error=\"invalid_token\"");
    }

    [Fact]
    public async Task Me_TokenExpirado_Retorna401InvalidToken()
    {
        var response = await _api.ComTokenExpirado().GetAsync("/me", Ct);

        var desafio = await response.DeveSer401ComDesafioBearerAsync();
        desafio.ShouldContain("error=\"invalid_token\"");
    }

    [Fact]
    public async Task Me_ToleranciaDeRelogio_AceitaAte30sDepoisDoExpERecusaDepois()
    {
        // Token de 5 min emitido "agora" (relógio falso). O MESMO token é reenviado conforme o tempo avança.
        var client = _api.ComoCliente(ApiFactory.Ana);
        (await client.GetAsync("/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);

        _api.Relogio.Advance(EmissorDeTokensDeTeste.ValidadePadrao + TimeSpan.FromSeconds(29));
        (await client.GetAsync("/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK,
            "29 s depois do exp ainda está dentro da tolerância de 30 s.");

        _api.Relogio.Advance(TimeSpan.FromSeconds(2));
        var response = await client.GetAsync("/me", Ct);
        var desafio = await response.DeveSer401ComDesafioBearerAsync();
        desafio.ShouldContain("error=\"invalid_token\"",
            customMessage: "31 s depois do exp: expirado. Se passou, a tolerância está no padrão de 5 min ou o relógio não é o injetado.");
    }
}
