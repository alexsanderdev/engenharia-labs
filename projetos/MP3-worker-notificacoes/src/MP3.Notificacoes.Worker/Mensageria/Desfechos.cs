using System.Diagnostics;

namespace MP3.Notificacoes.Worker.Mensageria;

/// <summary>Como uma mensagem foi liquidada pelo consumidor.</summary>
public enum Desfecho
{
    /// <summary>Notificação enviada; mensagem completada.</summary>
    Notificada,

    /// <summary>Evento já processado (inbox); mensagem completada sem reenviar.</summary>
    Duplicada,

    /// <summary>Falha transitória; uma cópia foi agendada com backoff e a original completada.</summary>
    Reagendada,

    /// <summary>Mensagem movida para a DLQ (falha permanente, contrato inválido ou tentativas esgotadas).</summary>
    DeadLetter,

    /// <summary>Processamento interrompido (desligamento); mensagem devolvida à fila.</summary>
    Abandonada,
}

/// <summary>
/// Ganchos chamados depois de cada liquidação. A implementação padrão alimenta métricas; os testes registram
/// um observador próprio para saber, sem adivinhar, o que aconteceu com cada mensagem.
/// </summary>
public interface IObservadorDeProcessamento
{
    void Registrar(string messageId, Desfecho desfecho, string? motivo = null);
}

public sealed class ObservadorDeMetricas : IObservadorDeProcessamento
{
    public void Registrar(string messageId, Desfecho desfecho, string? motivo = null) =>
        Telemetria.Telemetria.Desfechos.Add(1,
            new KeyValuePair<string, object?>("desfecho", desfecho.ToString()),
            new KeyValuePair<string, object?>("motivo", motivo));
}

internal static class AtividadeExtensions
{
    public static void Desfecho(this Activity? atividade, Desfecho desfecho, string? motivo = null)
    {
        atividade?.SetTag("mp3.desfecho", desfecho.ToString());
        if (motivo is not null) atividade?.SetTag("mp3.motivo", motivo);
        if (desfecho is Mensageria.Desfecho.DeadLetter) atividade?.SetStatus(ActivityStatusCode.Error, motivo);
    }
}
