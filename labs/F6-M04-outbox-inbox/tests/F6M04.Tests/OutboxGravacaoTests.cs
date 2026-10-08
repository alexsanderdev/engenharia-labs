using System.Text.Json;
using F6M04.Pedidos.Dominio;
using F6M04.Tests.Infra;
using Microsoft.EntityFrameworkCore;

namespace F6M04.Tests;

/// <summary>Passo 1 — o evento é gravado na Outbox na MESMA transação do agregado (e nada fala com o broker).</summary>
[Collection(ColecaoInfra.Nome)]
public sealed class OutboxGravacaoTests(InfraFixture infra) : TesteComInfra(infra)
{
    [Fact]
    public async Task CriarPedido_GravaOPedidoEOEventoNaOutbox_SemFalarComOBroker()
    {
        var pedidoId = await CriarPedidoAsync("PED-0001", total: 250m);

        Publicador.Publicadas.ShouldBeEmpty("o caso de uso não pode publicar direto (dual write): quem publica é o OutboxProcessor");

        var outbox = await LerOutboxAsync();
        var mensagem = outbox.ShouldHaveSingleItem();
        mensagem.Tipo.ShouldBe(PedidoCriado.NomeDoTipo);
        mensagem.ChaveDeOrdenacao.ShouldBe(pedidoId.ToString());
        mensagem.ProcessadoEm.ShouldBeNull();
        mensagem.Tentativas.ShouldBe(0);
        mensagem.VersaoDoAgregado.ShouldBe(1);
        mensagem.OcorridoEm.ShouldBe(Relogio.GetUtcNow());

        using var json = JsonDocument.Parse(mensagem.Payload);
        json.RootElement.GetProperty("pedidoId").GetGuid().ShouldBe(pedidoId);
        json.RootElement.GetProperty("numero").GetString().ShouldBe("PED-0001");
        json.RootElement.GetProperty("total").GetDecimal().ShouldBe(250m);
    }

    [Fact]
    public async Task ConfirmarPedido_GravaPedidoConfirmadoDepoisDoPedidoCriado_ComAMesmaChaveDeOrdenacao()
    {
        var pedidoId = await CriarPedidoAsync("PED-0001");
        await ConfirmarPedidoAsync(pedidoId);

        var outbox = await LerOutboxAsync();
        outbox.Select(m => m.Tipo).ShouldBe([PedidoCriado.NomeDoTipo, PedidoConfirmado.NomeDoTipo]);
        outbox.ShouldAllBe(m => m.ChaveDeOrdenacao == pedidoId.ToString());
        outbox.Select(m => m.VersaoDoAgregado).ShouldBe([1, 2], "a versão do agregado numera a história do pedido");
        outbox.Select(m => m.Id).ShouldBeUnique();
    }

    [Fact]
    public async Task CriarPedido_QuandoATransacaoDeNegocioFalha_NenhumEventoSai()
    {
        var primeiro = await CriarPedidoAsync("PED-0001");

        // Mesmo número: a constraint única derruba o commit. Pedido e evento são desfeitos JUNTOS.
        await Should.ThrowAsync<DbUpdateException>(() => CriarPedidoAsync("PED-0001"));

        var outbox = await LerOutboxAsync();
        outbox.ShouldHaveSingleItem().ChaveDeOrdenacao.ShouldBe(primeiro.ToString());

        // E, rodando o processor até esvaziar, só o evento do pedido que existe sai.
        await ProcessarAteEsvaziarAsync(CriarProcessor(Publicador));
        Publicador.Publicadas.ShouldHaveSingleItem().ChaveDeOrdenacao.ShouldBe(primeiro.ToString());
    }
}
