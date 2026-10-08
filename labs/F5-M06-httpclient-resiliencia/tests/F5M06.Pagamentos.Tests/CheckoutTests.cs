using System.Text.Json;
using F5M06.Pagamentos.Checkout;
using F5M06.Pagamentos.Gateway;
using F5M06.Pagamentos.Tests.Infra;
using static F5M06.Pagamentos.Tests.Infra.GatewayFake;

namespace F5M06.Pagamentos.Tests;

/// <summary>Passo 9 — o caso de uso não conhece HTTP e não vê exceção de integração.</summary>
public sealed class CheckoutTests
{
    [Fact]
    public async Task Pagar_Aprovado_ConfirmaOPedidoComChaveDeIdempotenciaDerivadaDoPedido()
    {
        using var c = new Cenario();
        c.Gateway.Sempre(PostCobranca(), Aprovada("tx_777"));
        var pedido = new Pedido(Guid.NewGuid(), 99.90m);

        var resultado = await c.Checkout.PagarAsync(pedido, "tok_teste_visa");

        resultado.Situacao.ShouldBe(SituacaoPagamento.Pago);
        pedido.Situacao.ShouldBe(SituacaoPagamento.Pago);
        pedido.TransacaoId.ShouldBe("tx_777");

        var requisicao = (await c.Gateway.RequisicoesAsync(RotaCobrancas, 1)).ShouldHaveSingleItem();
        Header(requisicao, ResilienciaGateway.CabecalhoIdempotencia).ShouldBe($"pedido-{pedido.Id:N}-pagamento-1");
        var corpo = JsonSerializer.Deserialize<CobrancaRequestDto>(requisicao.Body!, JsonSerializerOptions.Web)!;
        corpo.ShouldBe(new CobrancaRequestDto(pedido.Id, 99.90m, "BRL", "tok_teste_visa"));
    }

    [Fact]
    public async Task Pagar_RecusadoENovaTentativa_UsaOutraChaveDeIdempotencia()
    {
        using var c = new Cenario();
        c.Gateway.EmSequencia(PostCobranca(), Recusada("saldo_insuficiente"), Aprovada("tx_2"));
        var pedido = new Pedido(Guid.NewGuid(), 250m);

        var primeira = await c.Checkout.PagarAsync(pedido, "tok_cartao_1");
        var segunda = await c.Checkout.PagarAsync(pedido, "tok_cartao_2");

        primeira.ShouldBe(new ResultadoCheckout(SituacaoPagamento.Recusado, "saldo_insuficiente"));
        segunda.Situacao.ShouldBe(SituacaoPagamento.Pago);
        var chaves = (await c.Gateway.RequisicoesAsync(RotaCobrancas, 2))
            .Select(r => Header(r, ResilienciaGateway.CabecalhoIdempotencia)).ToList();
        chaves.ShouldBe([$"pedido-{pedido.Id:N}-pagamento-1", $"pedido-{pedido.Id:N}-pagamento-2"],
            "402 é resposta de negócio (sem retry); outro cartão é outra operação");
    }

    [Fact]
    public async Task Pagar_GatewayFora_NaoLancaEDeixaOPagamentoPendente()
    {
        using var c = new Cenario();
        c.Gateway.Sempre(PostCobranca(), Status(503));
        var pedido = new Pedido(Guid.NewGuid(), 80m);

        var resultado = await c.Checkout.PagarAsync(pedido, "tok_teste_visa");

        resultado.Situacao.ShouldBe(SituacaoPagamento.PagamentoPendente);
        pedido.Situacao.ShouldBe(SituacaoPagamento.PagamentoPendente);
        pedido.TentativaDePagamento.ShouldBe(1, "a cobrança pode ter acontecido: a próxima tentativa reusa a chave");
        var requisicoes = await c.Gateway.RequisicoesAsync(RotaCobrancas, 3);
        requisicoes.Count.ShouldBe(3);
        requisicoes.Select(r => Header(r, ResilienciaGateway.CabecalhoIdempotencia)).Distinct().ShouldHaveSingleItem();
    }
}
