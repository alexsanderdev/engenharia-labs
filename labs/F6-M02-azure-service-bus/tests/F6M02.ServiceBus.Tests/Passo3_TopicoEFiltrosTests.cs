using F6M02.ServiceBus.Consumo;
using F6M02.ServiceBus.Contratos;
using F6M02.ServiceBus.Publicacao;
using F6M02.ServiceBus.Tests.Infra;
using F6M02.ServiceBus.Topologia;

namespace F6M02.ServiceBus.Tests;

/// <summary>
/// Passo 3 — publicar no tópico e deixar o BROKER rotear pelas regras das subscriptions
/// (as regras ficam em src/F6M02.ServiceBus/Topologia/Config.json).
/// </summary>
/// <remarks>
/// Truque para provar "não chegou" sem esperar à toa: o último pedido publicado é uma SENTINELA que a
/// subscription deve receber. O tópico entrega na ordem de envio do mesmo sender; quando a sentinela chega,
/// tudo o que foi publicado antes dela já teria chegado também.
/// </remarks>
[Collection(ColecaoServiceBus.Nome)]
public sealed class Passo3_TopicoEFiltrosTests(ServiceBusFixture fixture) : IAsyncLifetime
{
    private static readonly OrigemDasMensagens Notificacao = OrigemDasMensagens.DaSubscription(Entidades.TopicoPedidos, Entidades.SubscriptionNotificacao);
    private static readonly OrigemDasMensagens Antifraude = OrigemDasMensagens.DaSubscription(Entidades.TopicoPedidos, Entidades.SubscriptionAntifraude);
    private static readonly OrigemDasMensagens Fidelidade = OrigemDasMensagens.DaSubscription(Entidades.TopicoPedidos, Entidades.SubscriptionFidelidade);

    public async ValueTask InitializeAsync() => await fixture.DrenarAsync(Notificacao, Antifraude, Fidelidade);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static string Id(PedidoCriado p) => $"pedido-criado-{p.PedidoId:N}";

    [Fact]
    public async Task Publicar_SubscriptionSemFiltroRecebeOPedidoComTodosOsMetadados()
    {
        await using var publicador = new PublicadorDePedidos(fixture.Cliente);
        var pedido = Novo.Pedido(valor: 321.50m, segmento: "vip", canal: "loja");
        var correlationId = Novo.CorrelationId();

        await publicador.PublicarAsync(pedido, correlationId);

        var recebidas = await Esperas.ReceberAte(fixture.Cliente, Notificacao, r => r.Any(m => m.MessageId == Id(pedido)));
        var mensagem = recebidas.ShouldHaveSingleItem();

        mensagem.MessageId.ShouldBe(Id(pedido));
        mensagem.CorrelationId.ShouldBe(correlationId);
        mensagem.Subject.ShouldBe("PedidoCriado");
        mensagem.ContentType.ShouldBe("application/json");
        mensagem.ApplicationProperties["valorTotal"].ShouldBe(321.50);
        mensagem.ApplicationProperties["segmento"].ShouldBe("vip");
        MensagensDePedido.LerPedidoCriado(mensagem).ShouldBe(pedido);
    }

    [Fact]
    public async Task Publicar_AntifraudeRecebeSoPedidosAcimaDe500_SqlFilter()
    {
        await using var publicador = new PublicadorDePedidos(fixture.Cliente);
        var barato = Novo.Pedido(valor: 100m);
        var quase = Novo.Pedido(valor: 499.99m);
        var exatamente500 = Novo.Pedido(valor: 500m);
        var acima = Novo.Pedido(valor: 500.01m);
        var sentinela = Novo.Pedido(valor: 1200m);

        foreach (var p in new[] { barato, quase, exatamente500, acima, sentinela })
            await publicador.PublicarAsync(p, Novo.CorrelationId());

        var recebidas = await Esperas.ReceberAte(fixture.Cliente, Antifraude, r => r.Any(m => m.MessageId == Id(sentinela)));

        recebidas.Select(m => m.MessageId).ShouldBe([Id(acima), Id(sentinela)],
            "a subscription antifraude só pode receber pedidos com valorTotal > 500 (regra SQL no Config.json)");

        // A subscription sem filtro continua recebendo todos.
        var todas = await Esperas.ReceberAte(fixture.Cliente, Notificacao, r => r.Count >= 5);
        todas.Count.ShouldBe(5);
    }

    [Fact]
    public async Task Publicar_FidelidadeRecebeSoPedidosDeClientesVip_CorrelationFilter()
    {
        await using var publicador = new PublicadorDePedidos(fixture.Cliente);
        var comumCaro = Novo.Pedido(valor: 900m, segmento: "comum");
        var vipBarato = Novo.Pedido(valor: 50m, segmento: "vip");
        var comumApp = Novo.Pedido(valor: 80m, segmento: "comum", canal: "app");
        var sentinelaVip = Novo.Pedido(valor: 2000m, segmento: "vip");

        foreach (var p in new[] { comumCaro, vipBarato, comumApp, sentinelaVip })
            await publicador.PublicarAsync(p, Novo.CorrelationId());

        var recebidas = await Esperas.ReceberAte(fixture.Cliente, Fidelidade, r => r.Any(m => m.MessageId == Id(sentinelaVip)));

        recebidas.Select(m => m.MessageId).ShouldBe([Id(vipBarato), Id(sentinelaVip)],
            "a subscription fidelidade só pode receber PedidoCriado com segmento = vip (correlation filter no Config.json)");
    }

    [Fact]
    public async Task PublicarLote_EnviaTodosOsPedidosEmUmUnicoLote()
    {
        await using var publicador = new PublicadorDePedidos(fixture.Cliente);
        var pedidos = Enumerable.Range(1, 30).Select(i => Novo.Pedido(valor: i * 10m)).ToList();

        var lotes = await publicador.PublicarLoteAsync(pedidos, Novo.CorrelationId());

        lotes.ShouldBe(1, "30 mensagens pequenas cabem num ServiceBusMessageBatch (256 KB no emulador/Standard)");
        var recebidas = await Esperas.ReceberAte(fixture.Cliente, Notificacao, r => r.Count >= 30);
        recebidas.Select(m => m.MessageId).ShouldBe(pedidos.Select(Id), ignoreOrder: true);
    }
}
