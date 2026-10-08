using System.Text;
using System.Text.Json.Nodes;
using F6M01.Mensageria.Contratos;
using F6M01.Mensageria.Topologia;
using Microsoft.Extensions.Time.Testing;

namespace F6M01.Mensageria.Tests;

/// <summary>Passos 1 e 2 — catálogo de contratos e envelope (sem broker).</summary>
public class ContratosTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 8, 14, 30, 15, 123, TimeSpan.Zero);
    private static readonly PedidoCriado Evento = new(Guid.NewGuid(), Guid.NewGuid(), 349.90m, Agora);

    // ---------- Catálogo: comando × evento ----------

    [Fact]
    public void Catalogo_ComandoReservarEstoque_VaiParaExchangeDeComandosComAFilaDoUnicoDestinatario()
    {
        var contrato = CatalogoDeContratos.De<ReservarEstoque>();

        contrato.Natureza.ShouldBe(NaturezaDaMensagem.Comando);
        contrato.Tipo.ShouldBe("orderflow.estoque.reservar-estoque");
        contrato.Versao.ShouldBe(1);
        contrato.Exchange.ShouldBe(TopologiaOrderFlow.ExchangeComandos);
        contrato.RoutingKey.ShouldBe(TopologiaOrderFlow.FilaReservarEstoque,
            "comando tem UM destinatário: a routing key é a fila dele");
    }

    [Fact]
    public void Catalogo_Eventos_VaoParaExchangeTopicComRoutingKeyDoFatoNoPassado()
    {
        var criado = CatalogoDeContratos.De<PedidoCriado>();
        var cancelado = CatalogoDeContratos.De<PedidoCancelado>();

        criado.Natureza.ShouldBe(NaturezaDaMensagem.Evento);
        criado.Tipo.ShouldBe("orderflow.pedidos.pedido-criado");
        criado.Exchange.ShouldBe(TopologiaOrderFlow.ExchangeEventos);
        criado.RoutingKey.ShouldBe("pedido.criado");

        cancelado.Natureza.ShouldBe(NaturezaDaMensagem.Evento);
        cancelado.Tipo.ShouldBe("orderflow.pedidos.pedido-cancelado");
        cancelado.RoutingKey.ShouldBe("pedido.cancelado");
    }

    [Fact]
    public void Catalogo_TodaMensagemDoAssemblyTemContratoCoerenteComSuaNatureza()
    {
        var mensagens = typeof(IMensagem).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IMensagem).IsAssignableFrom(t))
            .ToList();

        mensagens.ShouldNotBeEmpty();
        foreach (var tipo in mensagens)
        {
            var contrato = CatalogoDeContratos.De(tipo);
            var esperada = typeof(IComando).IsAssignableFrom(tipo) ? NaturezaDaMensagem.Comando : NaturezaDaMensagem.Evento;
            contrato.Natureza.ShouldBe(esperada, $"{tipo.Name} está com a natureza errada no catálogo");
            contrato.Tipo.ShouldNotContain(tipo.Namespace!, customMessage: "o tipo no fio não pode ser o nome .NET");
        }

        CatalogoDeContratos.Todos.Select(c => c.Tipo).ShouldBeUnique();
    }

    // ---------- Envelope ----------

    [Fact]
    public void CriarEnvelope_PreencheIdentidadeContratoInstanteECorpo()
    {
        var relogio = new FakeTimeProvider(Agora);

        var envelope = Envelope.Criar(Evento, "pedido-42", relogio);

        envelope.MessageId.ShouldNotBe(Guid.Empty);
        envelope.MessageId.Version.ShouldBe(7, "use Guid.CreateVersion7: único e ordenável no tempo");
        envelope.CorrelationId.ShouldBe("pedido-42");
        envelope.Tipo.ShouldBe("orderflow.pedidos.pedido-criado");
        envelope.Versao.ShouldBe(1);
        envelope.CriadoEm.ShouldBe(Agora);

        var json = JsonNode.Parse(envelope.Corpo)!;
        json["pedidoId"]!.GetValue<Guid>().ShouldBe(Evento.PedidoId);
        json["total"]!.GetValue<decimal>().ShouldBe(349.90m);
    }

    [Fact]
    public void CriarEnvelope_SemCorrelationId_IniciaUmFluxoNovoComOProprioMessageId_ECadaMensagemTemIdProprio()
    {
        var relogio = new FakeTimeProvider(Agora);

        var primeiro = Envelope.Criar(Evento, correlationId: null, relogio);
        var segundo = Envelope.Criar(Evento, correlationId: null, relogio);

        primeiro.CorrelationId.ShouldBe(primeiro.MessageId.ToString("D"));
        segundo.MessageId.ShouldNotBe(primeiro.MessageId, "a MESMA mensagem de negócio publicada 2× gera 2 MessageIds");
    }

    [Fact]
    public void LerCorpo_MudancaAditivaNoContrato_ConsumidorV1IgnoraCampoNovo()
    {
        // Um publicador mais novo acrescentou "canal" ao PedidoCriado v1 (mudança aditiva, compatível).
        var corpo = Encoding.UTF8.GetBytes(
            $$"""{"pedidoId":"{{Evento.PedidoId}}","clienteId":"{{Evento.ClienteId}}","total":349.90,"criadoEm":"2026-10-08T14:30:15.123+00:00","canal":"app"}""");
        var envelope = new Envelope(Guid.CreateVersion7(), "c-1", "orderflow.pedidos.pedido-criado", 1, Agora, corpo);

        var lido = envelope.LerCorpo<PedidoCriado>();

        lido.ShouldBe(Evento);
    }

    [Theory]
    [InlineData("orderflow.pedidos.pedido-criado", 2)]
    [InlineData("orderflow.pedidos.pedido-cancelado", 1)]
    public void LerCorpo_TipoOuVersaoQueOConsumidorNaoConhece_LancaContratoIncompativel(string tipo, int versao)
    {
        var envelope = Envelope.Criar(Evento, null, new FakeTimeProvider(Agora)) with { Tipo = tipo, Versao = versao };

        Should.Throw<ContratoIncompativelException>(() => envelope.LerCorpo<PedidoCriado>());
    }

    [Fact]
    public void LerCorpo_JsonCorrompido_LancaContratoIncompativel()
    {
        var envelope = Envelope.Criar(Evento, null, new FakeTimeProvider(Agora)) with { Corpo = "{nao é json"u8.ToArray() };

        Should.Throw<ContratoIncompativelException>(() => envelope.LerCorpo<PedidoCriado>());
    }
}
