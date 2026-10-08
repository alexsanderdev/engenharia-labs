using System.Text;
using F6M01.Mensageria.Contratos;
using Microsoft.Extensions.Time.Testing;
using RabbitMQ.Client;

namespace F6M01.Mensageria.Tests;

/// <summary>Passo 3 — envelope ↔ propriedades AMQP (sem broker).</summary>
public class MapeamentoAmqpTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 8, 14, 30, 15, 123, TimeSpan.Zero);

    private static Envelope NovoEnvelope() =>
        Envelope.Criar(new PedidoCancelado(Guid.NewGuid(), "cliente desistiu"), "pedido-7", new FakeTimeProvider(Agora));

    [Fact]
    public void ParaPropriedades_MensagemPersistenteComIdentidadeTipoEVersao()
    {
        var envelope = NovoEnvelope();

        var props = MapeamentoAmqp.ParaPropriedades(envelope);

        props.Persistent.ShouldBeTrue("fila durável não basta: a MENSAGEM também precisa ser persistente");
        props.DeliveryMode.ShouldBe(DeliveryModes.Persistent);
        props.ContentType.ShouldBe("application/json");
        props.MessageId.ShouldBe(envelope.MessageId.ToString("D"));
        props.CorrelationId.ShouldBe("pedido-7");
        props.Type.ShouldBe("orderflow.pedidos.pedido-cancelado");
        props.Timestamp.UnixTime.ShouldBe(Agora.ToUnixTimeSeconds());
        props.Headers.ShouldNotBeNull();
        props.Headers[MapeamentoAmqp.HeaderVersao].ShouldBe(1);
    }

    [Fact]
    public void ParaEnvelope_ComHeadersComoOBrokerDevolve_ReconstroiOEnvelopeIgual()
    {
        var original = NovoEnvelope();
        var props = MapeamentoAmqp.ParaPropriedades(original);
        // O broker devolve headers de texto como byte[] UTF-8 (não como string).
        props.Headers![MapeamentoAmqp.HeaderCriadoEm] =
            Encoding.UTF8.GetBytes((string)props.Headers[MapeamentoAmqp.HeaderCriadoEm]!);

        var reconstruido = MapeamentoAmqp.ParaEnvelope(props, original.Corpo);

        reconstruido.MessageId.ShouldBe(original.MessageId);
        reconstruido.CorrelationId.ShouldBe(original.CorrelationId);
        reconstruido.Tipo.ShouldBe(original.Tipo);
        reconstruido.Versao.ShouldBe(original.Versao);
        reconstruido.CriadoEm.ShouldBe(Agora, "o instante exato (com ms) vem do header, não do timestamp AMQP");
        reconstruido.Corpo.ShouldBe(original.Corpo);
    }

    [Fact]
    public void ParaEnvelope_MensagemSemMessageIdOuSemTipo_EhVenenosa()
    {
        var corpo = "{}"u8.ToArray();

        Should.Throw<ContratoIncompativelException>(() =>
            MapeamentoAmqp.ParaEnvelope(new BasicProperties { Type = "orderflow.pedidos.pedido-criado" }, corpo));
        Should.Throw<ContratoIncompativelException>(() =>
            MapeamentoAmqp.ParaEnvelope(new BasicProperties { MessageId = Guid.NewGuid().ToString() }, corpo));
    }
}
