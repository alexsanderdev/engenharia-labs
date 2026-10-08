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
    internal async Task ProcessarEntregaAsync(MensagemRecebida mensagem, ulong deliveryTag)
    {
        var canal = _canalDeConsumo!;
        try
        {
            Exception? ultimoErro = null;

            for (var tentativa = 0; tentativa <= _politica.RetentativasImediatas; tentativa++)
            {
                try
                {
                    await _manipulador(mensagem with { TentativaImediata = tentativa }, _parada.Token);
                    await canal.BasicAckAsync(deliveryTag, multiple: false);
                    return;
                }
                catch (OperationCanceledException) when (_parada.IsCancellationRequested)
                {
                    // Desligando: devolve para a fila sem contar tentativa.
                    await canal.BasicNackAsync(deliveryTag, multiple: false, requeue: true);
                    return;
                }
                catch (Exception erro)
                {
                    ultimoErro = erro;
                    if (_classificador.Classificar(erro) == TipoDeErro.Permanente)
                    {
                        await EnviarParaDlqAsync(mensagem, MotivoDeadLetter.ErroPermanente, erro);
                        await canal.BasicAckAsync(deliveryTag, multiple: false);
                        return;
                    }

                    Log.FalhaTransitoria(_logger, erro, mensagem.MessageId, _fila, tentativa);
                }
            }

            // Esgotou as imediatas: fila de espera do próximo nível, ou DLQ.
            var proximoNivel = mensagem.TentativasAtrasadas + 1;
            if (proximoNivel <= _politica.Atrasos.Count)
            {
                var propriedades = mensagem.PropriedadesParaRepublicar(h => h[Cabecalhos.Tentativas] = proximoNivel);
                await _publicacao!.PublicarAsync("", PoliticaDeRetry.NomeDaFilaDeEspera(_fila, proximoNivel), propriedades, mensagem.Corpo);
                Log.RetryAtrasado(_logger, mensagem.MessageId, proximoNivel, _politica.Atrasos[proximoNivel - 1]);
            }
            else
            {
                await EnviarParaDlqAsync(mensagem, MotivoDeadLetter.RetentativasEsgotadas, ultimoErro!);
            }

            await canal.BasicAckAsync(deliveryTag, multiple: false);
        }
        catch (Exception erroDeInfra) when (!_parada.IsCancellationRequested)
        {
            // Não conseguimos nem publicar a cópia: devolve a original para a fila (nada se perde).
            Log.FalhaDeInfraestrutura(_logger, erroDeInfra, mensagem.MessageId);
            if (canal.IsOpen)
                await canal.BasicNackAsync(deliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task EnviarParaDlqAsync(MensagemRecebida mensagem, MotivoDeadLetter motivo, Exception erro)
    {
        var descricao = $"{erro.GetType().Name}: {erro.Message}";
        if (descricao.Length > 500) descricao = descricao[..500];

        var propriedades = mensagem.PropriedadesParaRepublicar(h =>
        {
            h[Cabecalhos.Motivo] = motivo.ToString();
            h[Cabecalhos.Erro] = descricao;
            h[Cabecalhos.FilaDeOrigem] = _fila;
            h[Cabecalhos.FalhouEm] = _tempo.GetUtcNow().ToString("O");
        });

        await _publicacao!.PublicarAsync("", PoliticaDeRetry.NomeDaDlq(_fila), propriedades, mensagem.Corpo);
        Log.EnviadaParaDlq(_logger, erro, mensagem.MessageId, _fila, motivo);
    }

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
