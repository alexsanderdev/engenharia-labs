using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using F6M03.Kafka.Contratos;
using F6M03.Kafka.Tests.Infra;

namespace F6M03.Kafka.Tests;

/// <summary>Passo 1 — contrato do evento: chave, JSON e headers com versão. Não precisa de broker.</summary>
public sealed class ContratoTests
{
    [Fact]
    public void CriarMensagem_PedidoCriado_ChaveEhPedidoIdECorpoEhJsonCamelCase()
    {
        var evento = Apoio.NovoPedidoCriado(total: 259.90m);

        var mensagem = SerializadorDeEventos.CriarMensagem(evento);

        mensagem.Key.ShouldBe(evento.PedidoId.ToString());
        using var json = JsonDocument.Parse(mensagem.Value);
        json.RootElement.GetProperty("pedidoId").GetGuid().ShouldBe(evento.PedidoId);
        json.RootElement.GetProperty("total").GetDecimal().ShouldBe(259.90m);
        json.RootElement.TryGetProperty("clienteId", out _).ShouldBeTrue("use o tipo concreto do evento ao serializar");
    }

    [Fact]
    public void CriarMensagem_PedidoCriado_HeadersTrazemTipoVersaoEventoIdEContentType()
    {
        var evento = Apoio.NovoPedidoCriado();

        var headers = SerializadorDeEventos.CriarMensagem(evento).Headers;

        Cabecalhos.Ler(headers, Cabecalhos.TipoEvento).ShouldBe("PedidoCriado");
        Cabecalhos.Ler(headers, Cabecalhos.VersaoContrato).ShouldBe("1");
        Cabecalhos.Ler(headers, Cabecalhos.EventoId).ShouldBe(evento.EventoId.ToString());
        Cabecalhos.Ler(headers, Cabecalhos.ContentType).ShouldBe("application/json");
    }

    [Fact]
    public void Ler_MensagemCriadaPeloSerializador_DevolveOMesmoEventoComTipoConcreto()
    {
        var criado = Apoio.NovoPedidoCriado();
        var confirmado = Apoio.NovoPedidoConfirmado(criado.PedidoId);

        SerializadorDeEventos.Ler(SerializadorDeEventos.CriarMensagem(criado)).ShouldBe(criado);
        SerializadorDeEventos.Ler(SerializadorDeEventos.CriarMensagem(confirmado)).ShouldBeOfType<PedidoConfirmado>().ShouldBe(confirmado);
    }

    [Theory]
    [InlineData("PedidoCriado", "2", "{}")]          // versão futura que este consumidor não conhece
    [InlineData("PedidoCancelado", "1", "{}")]       // tipo desconhecido
    [InlineData("PedidoCriado", "1", "{ não é json")] // corpo corrompido
    public void Ler_ContratoQueOConsumidorNaoEntende_LancaContratoNaoSuportado(string tipo, string versao, string corpo)
    {
        var headers = new Headers();
        Cabecalhos.Escrever(headers, Cabecalhos.TipoEvento, tipo);
        Cabecalhos.Escrever(headers, Cabecalhos.VersaoContrato, versao);
        var mensagem = new Message<string, byte[]> { Key = "x", Value = Encoding.UTF8.GetBytes(corpo), Headers = headers };

        Should.Throw<ContratoNaoSuportadoException>(() => SerializadorDeEventos.Ler(mensagem));
    }
}
