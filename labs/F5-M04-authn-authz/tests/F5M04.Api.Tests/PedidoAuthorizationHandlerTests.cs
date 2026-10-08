using System.Security.Claims;
using F5M04.Api.Pedidos;
using F5M04.Api.Seguranca;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace F5M04.Api.Tests;

/// <summary>
/// Passo 3 — O handler de autorização baseada em recurso, isolado (sem HTTP).
/// Matriz: dono lê e cancela; Admin só lê; outro cliente nada; sem sub nada; operação desconhecida nada.
/// </summary>
public sealed class PedidoAuthorizationHandlerTests
{
    private static readonly Guid Ana = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Bruno = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static readonly Pedido PedidoDaAna = Pedido.Criar(Ana,
        [new ItemPedido(Guid.NewGuid(), "Teclado", 350m, 1)], DateTimeOffset.UnixEpoch);

    /// <summary>Monta o usuário como a API montaria com MapInboundClaims = false e RoleClaimType = "role".</summary>
    private static ClaimsPrincipal Usuario(string? sub, params string[] papeis)
    {
        var claims = new List<Claim>();
        if (sub is not null) claims.Add(new Claim(TiposDeClaim.Sub, sub));
        claims.AddRange(papeis.Select(p => new Claim(TiposDeClaim.Papel, p)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer", TiposDeClaim.Sub, TiposDeClaim.Papel));
    }

    public static TheoryData<string, string, bool> Matriz => new()
    {
        { "dono", OperacoesPedido.NomeLer, true },
        { "dono", OperacoesPedido.NomeCancelar, true },
        { "outro-cliente", OperacoesPedido.NomeLer, false },
        { "outro-cliente", OperacoesPedido.NomeCancelar, false },
        { "admin", OperacoesPedido.NomeLer, true },
        { "admin", OperacoesPedido.NomeCancelar, false },
        { "sem-sub", OperacoesPedido.NomeLer, false },
        { "dono", "Excluir", false },
    };

    [Theory]
    [MemberData(nameof(Matriz))]
    public async Task Handler_AplicaAMatrizDePermissoes(string quem, string operacao, bool esperado)
    {
        var usuario = quem switch
        {
            "dono" => Usuario(Ana.ToString(), Papeis.Cliente),
            "outro-cliente" => Usuario(Bruno.ToString(), Papeis.Cliente),
            "admin" => Usuario(Guid.NewGuid().ToString(), Papeis.Admin),
            _ => Usuario(sub: null, Papeis.Cliente),
        };
        var requisito = new OperationAuthorizationRequirement { Name = operacao };
        var contexto = new AuthorizationHandlerContext([requisito], usuario, PedidoDaAna);

        await new PedidoAuthorizationHandler().HandleAsync(contexto);

        contexto.HasSucceeded.ShouldBe(esperado);
        contexto.HasFailed.ShouldBeFalse("Negar = não chamar Succeed. Fail() bloquearia qualquer outro handler.");
    }
}
