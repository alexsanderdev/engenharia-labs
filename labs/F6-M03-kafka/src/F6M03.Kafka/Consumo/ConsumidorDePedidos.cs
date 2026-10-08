using Confluent.Kafka;
using F6M03.Kafka.Contratos;

namespace F6M03.Kafka.Consumo;

/// <summary>Uma mudança de atribuição de partições observada por este consumidor.</summary>
/// <param name="Tipo">"atribuidas" ou "revogadas".</param>
/// <param name="Particoes">Números das partições envolvidas.</param>
public sealed record EventoDeRebalance(string Tipo, IReadOnlyList<int> Particoes);

/// <summary>
/// Consumidor at-least-once: lê, processa e SÓ ENTÃO faz commit do offset. Se o processo cair entre o
/// processamento e o commit, a mensagem é entregue de novo (por isso o manipulador precisa ser idempotente).
/// Num worker real, <see cref="ProcessarProximoAsync"/> roda em laço dentro de um BackgroundService.
/// </summary>
public sealed class ConsumidorDePedidos : IDisposable
{
    private readonly IConsumer<string, byte[]> _consumer;
    private readonly IManipuladorDeEvento _manipulador;
    private readonly EncaminhadorDeFalhas? _encaminhador;
    private readonly List<EventoDeRebalance> _historico = [];
    private readonly Lock _trava = new();
    private bool _fechado;

    /// <param name="config">Use <see cref="CriarConfig"/>.</param>
    /// <param name="topicos">Tópicos assinados (ex.: o principal e o de retry).</param>
    /// <param name="manipulador">Regra de negócio.</param>
    /// <param name="encaminhador">Se informado, falhas vão para retry/DLQ e o offset é commitado; se null, a exceção sobe sem commit.</param>
    public ConsumidorDePedidos(
        ConsumerConfig config,
        IEnumerable<string> topicos,
        IManipuladorDeEvento manipulador,
        EncaminhadorDeFalhas? encaminhador = null)
    {
        _manipulador = manipulador;
        _encaminhador = encaminhador;
        _consumer = new ConsumerBuilder<string, byte[]>(config)
            .SetPartitionsAssignedHandler((_, particoes) => AoAtribuir(particoes))
            .SetPartitionsRevokedHandler((_, particoes) => AoRevogar(particoes))
            .Build();
        _consumer.Subscribe(topicos);
    }

    /// <summary>Partições atualmente atribuídas a ESTE consumidor dentro do grupo.</summary>
    public IReadOnlyList<int> ParticoesAtribuidas => [.. _consumer.Assignment.Select(p => p.Partition.Value).Order()];

    /// <summary>Histórico de rebalanceamentos vistos por este consumidor (preenchido nos Passos 5).</summary>
    public IReadOnlyList<EventoDeRebalance> HistoricoDeRebalance
    {
        get { lock (_trava) return [.. _historico]; }
    }

    /// <summary>
    /// Passo 4: configuração do consumidor at-least-once:
    /// <list type="bullet">
    /// <item><c>GroupId</c> = <paramref name="grupo"/> (consumidores com o mesmo grupo DIVIDEM as partições).</item>
    /// <item><c>EnableAutoCommit = false</c> e <c>EnableAutoOffsetStore = false</c>: nada de commit automático em
    /// segundo plano — o commit é explícito, depois do processamento.</item>
    /// <item><c>AutoOffsetReset</c> = <paramref name="inicio"/>: onde começar quando o grupo AINDA NÃO TEM offset
    /// commitado (Earliest = do início do log; Latest = só o que chegar daqui para frente).</item>
    /// <item><c>ClientId</c> = "orderflow-worker".</item>
    /// </list>
    /// </summary>
    public static ConsumerConfig CriarConfig(string bootstrapServers, string grupo, AutoOffsetReset inicio = AutoOffsetReset.Earliest) => new()
    {
        BootstrapServers = bootstrapServers,
        GroupId = grupo,
        ClientId = "orderflow-worker",
        EnableAutoCommit = false,
        EnableAutoOffsetStore = false,
        AutoOffsetReset = inicio,
    };

    /// <summary>
    /// Passo 5: chamado pelo client quando o grupo atribui partições a este consumidor.
    /// Registre um <see cref="EventoDeRebalance"/>("atribuidas", números das partições) no histórico (use a trava).
    /// </summary>
    private void AoAtribuir(List<TopicPartition> particoes)
    {
        lock (_trava) _historico.Add(new EventoDeRebalance("atribuidas", [.. particoes.Select(p => p.Partition.Value)]));
    }

    /// <summary>
    /// Passo 5: chamado quando o grupo TIRA partições deste consumidor (outro entrou ou saiu).
    /// Registre um <see cref="EventoDeRebalance"/>("revogadas", ...). Num consumidor com lote em memória,
    /// aqui seria o lugar de terminar/commitar o que já foi processado.
    /// </summary>
    private void AoRevogar(List<TopicPartitionOffset> particoes)
    {
        lock (_trava) _historico.Add(new EventoDeRebalance("revogadas", [.. particoes.Select(p => p.Partition.Value)]));
    }

    /// <summary>
    /// Passo 4 (e 7): processa no máximo UMA mensagem.
    /// <list type="number">
    /// <item><c>Consume(espera)</c>; se vier null (nada chegou no tempo), devolva <c>false</c>.</item>
    /// <item>Leia o evento com <see cref="SerializadorDeEventos.Ler"/> e chame o manipulador.</item>
    /// <item>Se tudo deu certo: <c>Commit(resultado)</c> e devolva <c>true</c>.</item>
    /// <item>Passo 7: se falhar e houver encaminhador, encaminhe (retry/DLQ), faça commit e devolva <c>true</c>.
    /// Sem encaminhador, deixe a exceção subir SEM commit (a mensagem será relida após reinício/rebalance).
    /// Nunca trate <see cref="OperationCanceledException"/> como falha da mensagem.</item>
    /// </list>
    /// </summary>
    public async Task<bool> ProcessarProximoAsync(TimeSpan espera, CancellationToken ct = default)
    {
        var resultado = _consumer.Consume(espera);
        if (resultado is null) return false;

        try
        {
            var evento = SerializadorDeEventos.Ler(resultado.Message);
            await _manipulador.ManipularAsync(evento, ct);
        }
        catch (Exception ex) when (_encaminhador is not null && ex is not OperationCanceledException)
        {
            await _encaminhador.EncaminharAsync(resultado, ex, ct);
        }

        _consumer.Commit(resultado);
        return true;
    }

    /// <summary>Faz uma volta do laço de consumo sem esperar mensagem (útil para o grupo completar o rebalance).</summary>
    public Task<bool> GirarAsync() => ProcessarProximoAsync(TimeSpan.FromMilliseconds(50));

    /// <summary>
    /// Sai do grupo de forma educada (LeaveGroup → rebalance imediato). Um processo que MORRE não chama isto:
    /// o grupo só percebe depois de <c>session.timeout.ms</c> (45 s por padrão).
    /// </summary>
    public void Dispose()
    {
        if (_fechado) return;
        _fechado = true;
        _consumer.Close();
        _consumer.Dispose();
    }
}
