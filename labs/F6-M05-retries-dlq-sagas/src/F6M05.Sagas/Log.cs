using F6M05.Sagas.Mensageria;
using F6M05.Sagas.Saga;
using Microsoft.Extensions.Logging;

namespace F6M05.Sagas;

/// <summary>
/// PRONTO. Mensagens de log (source generator <c>LoggerMessage</c>). Todas carregam o
/// MessageId e/ou o PedidoId: são as chaves para seguir uma mensagem e uma saga nos logs.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "Falha transitória em {MessageId} (fila {Fila}, tentativa imediata {Tentativa})")]
    public static partial void FalhaTransitoria(ILogger logger, Exception erro, string messageId, string fila, int tentativa);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning,
        Message = "Mensagem {MessageId} agendada para a retentativa atrasada {Nivel} (espera {Atraso})")]
    public static partial void RetryAtrasado(ILogger logger, string messageId, int nivel, TimeSpan atraso);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error,
        Message = "Mensagem {MessageId} enviada para a DLQ de {Fila}: {Motivo}")]
    public static partial void EnviadaParaDlq(ILogger logger, Exception erro, string messageId, string fila, MotivoDeadLetter motivo);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error,
        Message = "Falha de infraestrutura ao tratar {MessageId}; devolvendo para a fila")]
    public static partial void FalhaDeInfraestrutura(ILogger logger, Exception erro, string messageId);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Conflito de concorrência na saga {PedidoId} (versão {Versao}) ao tratar {Tipo}; recarregando")]
    public static partial void ConflitoNaSaga(ILogger logger, Guid pedidoId, int versao, string tipo);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information,
        Message = "Mensagem {MessageId} duplicada na saga {PedidoId}; comandos do estado {Status} republicados")]
    public static partial void MensagemDuplicada(ILogger logger, string messageId, Guid pedidoId, StatusSaga status);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information,
        Message = "{Tipo} ignorada na saga {PedidoId} em {Status}")]
    public static partial void MensagemIgnorada(ILogger logger, string tipo, Guid pedidoId, StatusSaga status);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information,
        Message = "Saga {PedidoId}: {Anterior} --{Tipo}--> {Novo} (v{Versao})")]
    public static partial void Transicao(ILogger logger, Guid pedidoId, StatusSaga anterior, string tipo, StatusSaga novo, int versao);
}
