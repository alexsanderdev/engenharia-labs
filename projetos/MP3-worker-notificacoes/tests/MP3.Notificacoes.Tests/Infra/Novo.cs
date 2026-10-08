using Azure.Messaging.ServiceBus;
using MP3.Contratos;

namespace MP3.Notificacoes.Tests.Infra;

/// <summary>Dados de teste: cada chamada gera ids novos, então testes não colidem entre si.</summary>
public static class Novo
{
    public static PedidoCriado Pedido(string? canal = null, decimal valor = 259.90m) => new(
        EventoId: Guid.NewGuid(),
        PedidoId: Guid.NewGuid(),
        ClienteId: Guid.NewGuid(),
        NomeDoCliente: "Ana Souza",
        Email: "ana@example.com",
        Telefone: "+5511912345678",
        CanalPreferido: canal,
        ValorTotal: valor,
        CriadoEm: new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));

    /// <summary>A mensagem como a API do OrderFlow publica: MessageId = EventoId, Subject, ContentType e (opcional) traceparent.</summary>
    public static ServiceBusMessage Mensagem(PedidoCriado evento, string? traceparent = null)
    {
        var mensagem = new ServiceBusMessage(ConvencoesDeMensagem.Serializar(evento))
        {
            MessageId = evento.EventoId.ToString(),
            Subject = ConvencoesDeMensagem.Assunto,
            ContentType = ConvencoesDeMensagem.ContentType,
            CorrelationId = evento.PedidoId.ToString(),
        };
        if (traceparent is not null) mensagem.ApplicationProperties[ConvencoesDeMensagem.PropriedadeTraceparent] = traceparent;
        return mensagem;
    }
}
