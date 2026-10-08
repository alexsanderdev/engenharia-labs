using System.Text;
using F6M04.Pedidos.Mensageria;
using F6M04.Tests.Infra;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;

namespace F6M04.Tests;

/// <summary>Passo 2 — publicar no RabbitMQ COM publisher confirms (e sem "publicar no vazio").</summary>
[Collection(ColecaoInfra.Nome)]
public sealed class PublicadorRabbitMqTests(InfraFixture infra) : TesteComInfra(infra)
{
    private PublicadorRabbitMq CriarPublicador() => new(Options.Create(new RabbitMqOptions
    {
        ConnectionString = Infra.AmqpUri,
        Exchange = Topologia.Exchange,
    }));

    [Fact]
    public async Task PublicarAsync_ComConfirmacao_EntregaNaFilaComMessageIdTipoEModoPersistente()
    {
        await using var publicador = CriarPublicador();
        var mensagem = new MensagemDeSaida(Guid.CreateVersion7(), "pedido.criado", "pedido-123", """{"pedidoId":"123"}""", Relogio.GetUtcNow());

        await publicador.PublicarAsync(mensagem);

        var lida = (await Broker.LerAsync(Topologia.Fila, 1)).ShouldHaveSingleItem();
        lida.MessageId.ShouldBe(mensagem.MessageId.ToString());
        lida.Tipo.ShouldBe("pedido.criado");
        lida.Persistente.ShouldBeTrue("mensagem de negócio precisa sobreviver a um restart do broker");
        lida.ContentType.ShouldBe("application/json");
        lida.Corpo.ShouldBe(mensagem.Payload);
        lida.Headers.ShouldNotBeNull();
        var chave = lida.Headers[PublicadorRabbitMq.HeaderChaveDeOrdenacao].ShouldBeOfType<byte[]>();
        Encoding.UTF8.GetString(chave).ShouldBe("pedido-123");
    }

    [Fact]
    public async Task PublicarAsync_SemNenhumaFilaParaARoutingKey_FalhaEmVezDeSumirComAMensagem()
    {
        await using var publicador = CriarPublicador();

        // Nenhuma fila assina "estoque.*" nesta exchange: sem mandatory + confirms, a mensagem sumiria em silêncio
        // e a Outbox a marcaria como publicada.
        var mensagem = new MensagemDeSaida(Guid.CreateVersion7(), "estoque.reservado", "pedido-123", "{}", Relogio.GetUtcNow());

        var erro = await Should.ThrowAsync<PublishException>(() => publicador.PublicarAsync(mensagem));
        erro.IsReturn.ShouldBeTrue();
    }
}
