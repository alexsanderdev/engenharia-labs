using System.Net;
using System.Net.Http.Json;
using F5M03.Api.Dominio;
using F5M03.Api.Tests.Infra;
using static F5M03.Api.Tests.Infra.Dados;

namespace F5M03.Api.Tests;

/// <summary>
/// Ataque 1 — mass assignment (OWASP API3:2023, Broken Object Property Level Authorization).
/// O atacante manda no corpo campos que o contrato "não deveria" ter. Os testes aceitam
/// duas correções válidas: ignorar o campo (201) ou rejeitar o corpo (400) — o que não
/// pode acontecer é o campo chegar à entidade.
/// </summary>
public sealed class MassAssignmentTests : TesteDeApi
{
    [Fact]
    public async Task Cadastro_ComIsAdminNoCorpo_NaoCriaAdministrador()
    {
        var ataque = new { nome = "Mallory", email = "mallory@exemplo.com", cpf = CpfValido, senha = SenhaValida, isAdmin = true };

        var resposta = await Client.PostAsJsonAsync("/clientes", ataque, Ct);

        resposta.StatusCode.ShouldBeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
        Api.Clientes.Todos().ShouldAllBe(c => !c.IsAdmin, "o cadastro público jamais pode criar administrador");
    }

    [Fact]
    public async Task CriarPedido_ComPrecoTotalEStatusNoCorpo_ServidorIgnoraEUsaOCatalogo()
    {
        var ataque = new
        {
            clienteId = Guid.NewGuid(),
            itens = new[] { new { produtoId = ProdutosConhecidos.MonitorId, quantidade = 2, precoUnitario = 0.01m } },
            total = 0.02m,
            status = "Completed",
            custoTotal = 0m,
            observacaoInterna = "desconto aprovado pelo gerente :)",
        };

        var resposta = await Client.PostAsJsonAsync("/pedidos", ataque, Ct);

        resposta.StatusCode.ShouldBeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
        foreach (var pedido in Api.Pedidos.Todos())
        {
            pedido.Status.ShouldBe(StatusPedido.Created, "status inicial é decisão do servidor");
            pedido.Total.ShouldBe(2598.00m, "total vem do preço de catálogo, nunca do corpo");
            pedido.Itens.ShouldAllBe(i => i.PrecoUnitario == 1299.00m);
            pedido.ObservacaoInterna.ShouldNotBe("desconto aprovado pelo gerente :)");
        }
    }
}
