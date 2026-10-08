using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Publicacao;
using F6M02.ServiceBus.Tests.Infra;

namespace F6M02.ServiceBus.Tests;

/// <summary>Passo 1 — o contrato do fio. Sem broker: só montar e ler mensagens.</summary>
public sealed class Passo1_MensagensTests
{
    [Fact]
    public void CriarPedidoCriado_PreencheMetadadosEApplicationPropertiesUsadasNosFiltros()
    {
        var pedido = Novo.Pedido(valor: 789.90m, segmento: "vip", canal: "app");

        var mensagem = MensagensDePedido.CriarPedidoCriado(pedido, "corr-123");

        mensagem.MessageId.ShouldBe($"pedido-criado-{pedido.PedidoId:N}", "MessageId determinístico (derivado do pedido), não Guid aleatório");
        mensagem.CorrelationId.ShouldBe("corr-123");
        mensagem.Subject.ShouldBe("PedidoCriado");
        mensagem.ContentType.ShouldBe("application/json");

        mensagem.ApplicationProperties["tipo"].ShouldBe("PedidoCriado");
        mensagem.ApplicationProperties["valorTotal"].ShouldBeOfType<double>().ShouldBe(789.90, 0.001);
        mensagem.ApplicationProperties["segmento"].ShouldBe("vip");
        mensagem.ApplicationProperties["canal"].ShouldBe("app");
        mensagem.ApplicationProperties["clienteId"].ShouldBe(pedido.ClienteId.ToString());
        mensagem.ApplicationProperties["versao"].ShouldBe(1);

        // O mesmo pedido gera sempre o mesmo MessageId (base da detecção de duplicatas).
        MensagensDePedido.CriarPedidoCriado(pedido, "outro-corr").MessageId.ShouldBe(mensagem.MessageId);
    }

    [Fact]
    public void LerPedidoCriado_CorpoJsonDoContrato_DevolveOEvento()
    {
        var pedido = Novo.Pedido(valor: 1234.56m);
        var enviada = MensagensDePedido.CriarPedidoCriado(pedido, "corr-1");

        // O corpo é JSON camelCase (contrato legível por qualquer linguagem).
        enviada.Body.ToString().ShouldContain("\"pedidoId\"");
        enviada.Body.ToString().ShouldContain("\"valorTotal\":1234.56");

        var recebida = ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: enviada.Body, messageId: enviada.MessageId, contentType: enviada.ContentType);

        MensagensDePedido.LerPedidoCriado(recebida).ShouldBe(pedido);
    }

    [Fact]
    public void LerPedidoCriado_ContentTypeOuCorpoInvalido_LancaMensagemInvalida()
    {
        var corpoBom = MensagensDePedido.CriarPedidoCriado(Novo.Pedido(), "c").Body;

        var textoPuro = ServiceBusModelFactory.ServiceBusReceivedMessage(body: corpoBom, contentType: "text/plain");
        var jsonQuebrado = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{ isto não é json"), contentType: "application/json");
        var semPedidoId = ServiceBusModelFactory.ServiceBusReceivedMessage(body: BinaryData.FromString("{\"valorTotal\": 10}"), contentType: "application/json");

        Should.Throw<MensagemInvalidaException>(() => MensagensDePedido.LerPedidoCriado(textoPuro));
        Should.Throw<MensagemInvalidaException>(() => MensagensDePedido.LerPedidoCriado(jsonQuebrado));
        Should.Throw<MensagemInvalidaException>(() => MensagensDePedido.LerPedidoCriado(semPedidoId));
    }
}
