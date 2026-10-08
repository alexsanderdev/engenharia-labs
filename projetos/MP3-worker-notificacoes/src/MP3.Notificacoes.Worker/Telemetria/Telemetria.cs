using System.Diagnostics;
using System.Diagnostics.Metrics;
using Azure.Messaging.ServiceBus;
using MP3.Contratos;

namespace MP3.Notificacoes.Worker.Telemetria;

/// <summary>
/// Instrumentação com as APIs do próprio .NET (<see cref="ActivitySource"/> e <see cref="Meter"/>).
/// O OpenTelemetry só "escuta" estas fontes: plugar o exporter (OTLP, Azure Monitor) é configuração,
/// não muda este código — veja a etapa opcional de telemetria no README.
/// </summary>
public static class Telemetria
{
    public const string Nome = "MP3.Notificacoes";

    public static readonly ActivitySource Fonte = new(Nome);

    private static readonly Meter Medidor = new(Nome);

    public static readonly Counter<long> Desfechos =
        Medidor.CreateCounter<long>("mp3.notificacoes.desfechos", description: "Mensagens liquidadas, por desfecho.");

    /// <summary>
    /// Inicia o span de processamento CONTINUANDO o trace da API: o pai vem da propriedade
    /// <c>traceparent</c> (W3C) da mensagem — ou de <c>Diagnostic-Id</c>, que é onde o SDK do Service Bus
    /// grava o contexto quando a instrumentação dele está ligada.
    /// </summary>
    public static Activity? IniciarProcessamento(ServiceBusReceivedMessage mensagem, string fila)
    {
        var pai = default(ActivityContext);
        foreach (var chave in new[] { ConvencoesDeMensagem.PropriedadeTraceparent, "Diagnostic-Id" })
        {
            if (mensagem.ApplicationProperties.TryGetValue(chave, out var valor)
                && valor is string texto
                && ActivityContext.TryParse(texto, null, out pai))
            {
                break;
            }
        }

        var atividade = Fonte.StartActivity($"{fila} process", ActivityKind.Consumer, pai);
        atividade?.SetTag("messaging.system", "servicebus");
        atividade?.SetTag("messaging.operation.type", "process");
        atividade?.SetTag("messaging.destination.name", fila);
        atividade?.SetTag("messaging.message.id", mensagem.MessageId);
        atividade?.SetTag("messaging.servicebus.message.delivery_count", mensagem.DeliveryCount);
        return atividade;
    }
}
