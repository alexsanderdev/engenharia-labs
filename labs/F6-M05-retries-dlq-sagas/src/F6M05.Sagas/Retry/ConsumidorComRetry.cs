using F6M05.Sagas.Mensageria;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace F6M05.Sagas.Retry;

/// <summary>
/// Consumidor de uma fila do RabbitMQ com retry imediato, retry atrasado (fila de espera com
/// TTL), classificação de erro e dead-letter queue com motivo.
/// </summary>
/// <remarks>
/// Garantia: <b>at-least-once</b>. O ack da entrega original só sai DEPOIS que a cópia foi
/// confirmada pelo broker na fila de espera/DLQ. Se o processo cair entre os dois, a mensagem é
/// reentregue e também existe a cópia: o manipulador precisa ser idempotente.
/// </remarks>
public sealed class ConsumidorComRetry : IAsyncDisposable
{
    private readonly IConnection _conexao;
    private readonly string _fila;
    private readonly PoliticaDeRetry _politica;
    private readonly Func<MensagemRecebida, CancellationToken, Task> _manipulador;
    private readonly ClassificadorDeErros _classificador;
    private readonly TimeProvider _tempo;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _parada = new();

    private IChannel? _canalDeConsumo;
    private CanalDePublicacao? _publicacao;
    private string? _consumerTag;

    /// <param name="conexao">Conexão AMQP compartilhada (uma por processo).</param>
    /// <param name="fila">Fila principal (a topologia já deve ter sido declarada com <see cref="TopologiaDeRetry"/>).</param>
    /// <param name="politica">Retentativas imediatas e atrasadas.</param>
    /// <param name="manipulador">O processamento de negócio. Lançar exceção = falha.</param>
    /// <param name="classificador">Transitório × permanente (padrão: <see cref="ClassificadorDeErros"/>).</param>
    /// <param name="tempo">Relógio para o header <c>x-falhou-em</c>.</param>
    /// <param name="logger">Log de retry/DLQ.</param>
    public ConsumidorComRetry(
        IConnection conexao,
        string fila,
        PoliticaDeRetry politica,
        Func<MensagemRecebida, CancellationToken, Task> manipulador,
        ClassificadorDeErros? classificador = null,
        TimeProvider? tempo = null,
        ILogger? logger = null)
    {
        _conexao = conexao;
        _fila = fila;
        _politica = politica.Validar();
        _manipulador = manipulador;
        _classificador = classificador ?? new ClassificadorDeErros();
        _tempo = tempo ?? TimeProvider.System;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Abre os canais e começa a consumir. PRONTO.</summary>
    public async Task IniciarAsync(CancellationToken ct = default)
    {
        _canalDeConsumo = await _conexao.CreateChannelAsync(cancellationToken: ct);
        await _canalDeConsumo.BasicQosAsync(prefetchSize: 0, prefetchCount: _politica.Prefetch, global: false, ct);
        _publicacao = await CanalDePublicacao.CriarAsync(_conexao, ct);

        var consumidor = new AsyncEventingBasicConsumer(_canalDeConsumo);
        consumidor.ReceivedAsync += (_, entrega) => ProcessarEntregaAsync(
            MensagemRecebida.De(_fila, entrega.BasicProperties, entrega.Body, entrega.Redelivered),
            entrega.DeliveryTag);

        _consumerTag = await _canalDeConsumo.BasicConsumeAsync(_fila, autoAck: false, consumidor, ct);
    }

    /// <summary>
    /// O coração do lab: executa o manipulador e decide entre ack, retry imediato, fila de espera
    /// ou DLQ.
    /// </summary>
    /// <remarks>
    /// ESTADO ATUAL (o "antes"): sem retry e sem DLQ. Qualquer exceção vira
    /// <c>nack(requeue: false)</c>: sem dead-letter configurado, a mensagem simplesmente SOME.
    /// (A alternativa ingênua, <c>requeue: true</c>, é pior: a mensagem venenosa volta para a
    /// cabeça da fila e gira para sempre, queimando CPU e travando as outras.)
    /// <para>
    /// TODO (Passo 3):
    /// <list type="number">
    /// <item>Laço de <c>0..RetentativasImediatas</c>: chame o manipulador com
    /// <c>mensagem with { TentativaImediata = tentativa }</c> e o <c>_parada.Token</c>; sucesso → ack e fim.</item>
    /// <item><c>OperationCanceledException</c> com <c>_parada</c> cancelado (desligando): nack com requeue e fim.</item>
    /// <item>Erro <see cref="TipoDeErro.Permanente"/> (via <c>_classificador</c>): DLQ com
    /// <see cref="MotivoDeadLetter.ErroPermanente"/>, ack e fim. Transitório: log e próxima tentativa.</item>
    /// <item>Esgotou as imediatas: se <c>TentativasAtrasadas + 1 ≤ Atrasos.Count</c>, publique a cópia
    /// (<see cref="MensagemRecebida.PropriedadesParaRepublicar"/> com <see cref="Cabecalhos.Tentativas"/> = próximo nível)
    /// em <see cref="PoliticaDeRetry.NomeDaFilaDeEspera"/> pelo exchange padrão (<c>""</c>); senão, DLQ com
    /// <see cref="MotivoDeadLetter.RetentativasEsgotadas"/>. Depois, ack da original.</item>
    /// <item>Se publicar a cópia falhar (erro de infraestrutura), nack COM requeue: nada se perde.</item>
    /// </list>
    /// </para>
    /// </remarks>
    internal async Task ProcessarEntregaAsync(MensagemRecebida mensagem, ulong deliveryTag)
    {
        var canal = _canalDeConsumo!;
        try
        {
            await _manipulador(mensagem, _parada.Token);
            await canal.BasicAckAsync(deliveryTag, multiple: false);
        }
        catch (Exception erro)
        {
            Log.FalhaTransitoria(_logger, erro, mensagem.MessageId, _fila, 0);
            await canal.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
        }
    }

    /// <summary>
    /// Publica a cópia da mensagem na DLQ (<see cref="PoliticaDeRetry.NomeDaDlq"/>, exchange padrão) com os
    /// headers <see cref="Cabecalhos.Motivo"/> (nome do enum), <see cref="Cabecalhos.Erro"/>
    /// (<c>"{Tipo}: {Message}"</c>, até 500 caracteres), <see cref="Cabecalhos.FilaDeOrigem"/> e
    /// <see cref="Cabecalhos.FalhouEm"/> (<c>_tempo.GetUtcNow().ToString("O")</c>), preservando MessageId e corpo.
    /// </summary>
    private Task EnviarParaDlqAsync(MensagemRecebida mensagem, MotivoDeadLetter motivo, Exception erro) =>
        throw new NotImplementedException("TODO: publique a cópia na DLQ com motivo, erro, fila de origem e instante (Passo 3).");

    /// <summary>Para de consumir e fecha os canais. PRONTO.</summary>
    public async ValueTask DisposeAsync()
    {
        await _parada.CancelAsync();
        if (_canalDeConsumo is { IsOpen: true } && _consumerTag is not null)
        {
            try { await _canalDeConsumo.BasicCancelAsync(_consumerTag); }
            catch (Exception) { /* canal já fechando */ }
        }
        if (_canalDeConsumo is not null) await _canalDeConsumo.DisposeAsync();
        if (_publicacao is not null) await _publicacao.DisposeAsync();
        _parada.Dispose();
    }
}
