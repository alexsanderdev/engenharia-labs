using System.Collections.Concurrent;
using System.Diagnostics;
using Confluent.Kafka;
using F6M03.Kafka.Consumo;
using F6M03.Kafka.Contratos;

namespace F6M03.Kafka.Tests.Infra;

/// <summary>Helpers de teste. Pronto: leia, não altere.</summary>
public static class Apoio
{
    public static readonly TimeSpan TimeoutPadrao = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Repete <paramref name="acao"/> até ela devolver true ou estourar o tempo. É assim que se espera
    /// algo assíncrono num broker: polling com timeout curto, nunca um Task.Delay fixo "torcendo".
    /// </summary>
    public static async Task Eventualmente(Func<Task<bool>> acao, string descricao, TimeSpan? timeout = null)
    {
        var limite = timeout ?? TimeoutPadrao;
        var relogio = Stopwatch.StartNew();
        while (relogio.Elapsed < limite)
        {
            if (await acao()) return;
            await Task.Yield();
        }
        throw new TimeoutException($"Não aconteceu em {limite.TotalSeconds:0} s: {descricao}");
    }

    public static PedidoCriado NovoPedidoCriado(Guid? pedidoId = null, decimal total = 100m) =>
        new(Guid.NewGuid(), pedidoId ?? Guid.NewGuid(), Guid.NewGuid(), total, DateTimeOffset.UtcNow);

    public static PedidoConfirmado NovoPedidoConfirmado(Guid pedidoId) =>
        new(Guid.NewGuid(), pedidoId, DateTimeOffset.UtcNow);

    /// <summary>
    /// Lê TODAS as mensagens de um tópico desde o início, sem grupo (assign manual em todas as partições),
    /// até ter <paramref name="quantidade"/> mensagens ou estourar o tempo.
    /// </summary>
    public static List<ConsumeResult<string, byte[]>> LerDoInicio(string bootstrap, string topico, int quantidade, TimeSpan? timeout = null)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrap }).Build();
        var particoes = admin.GetMetadata(topico, TimeSpan.FromSeconds(10)).Topics.Single().Partitions;

        using var consumer = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "leitor-de-teste", // obrigatório no client, mas não usamos commit nem subscribe
            EnableAutoCommit = false,
        }).Build();
        consumer.Assign(particoes.Select(p => new TopicPartitionOffset(topico, p.PartitionId, Offset.Beginning)));

        var lidas = new List<ConsumeResult<string, byte[]>>();
        var relogio = Stopwatch.StartNew();
        var limite = timeout ?? TimeoutPadrao;
        while (lidas.Count < quantidade && relogio.Elapsed < limite)
        {
            var r = consumer.Consume(TimeSpan.FromMilliseconds(200));
            if (r is not null && !r.IsPartitionEOF) lidas.Add(r);
        }
        consumer.Close();
        return lidas;
    }

    /// <summary>Dá voltas no laço de consumo de vários consumidores até a condição valer (rebalance precisa disso).</summary>
    public static Task GirarAte(Func<bool> condicao, string descricao, params ConsumidorDePedidos[] consumidores) =>
        Eventualmente(async () =>
        {
            foreach (var c in consumidores) await c.GirarAsync();
            return condicao();
        }, descricao);
}

/// <summary>Manipulador de teste: registra o que processou e pode falhar de propósito.</summary>
public sealed class ManipuladorDeTeste : IManipuladorDeEvento
{
    private readonly ConcurrentQueue<IEventoDePedido> _processados = new();
    private readonly ConcurrentDictionary<Guid, int> _falhasRestantes = new();
    private readonly ConcurrentDictionary<Guid, int> _tentativas = new();

    /// <summary>Eventos processados com sucesso, na ordem.</summary>
    public IReadOnlyList<IEventoDePedido> Processados => [.. _processados];

    /// <summary>Quantas vezes o manipulador foi chamado para cada pedido (sucesso + falha).</summary>
    public int TentativasDo(Guid pedidoId) => _tentativas.GetValueOrDefault(pedidoId);

    /// <summary>Faz o pedido falhar nas próximas <paramref name="vezes"/> chamadas (int.MaxValue = sempre).</summary>
    public void FalharPara(Guid pedidoId, int vezes) => _falhasRestantes[pedidoId] = vezes;

    public Task ManipularAsync(IEventoDePedido evento, CancellationToken ct)
    {
        _tentativas.AddOrUpdate(evento.PedidoId, 1, (_, n) => n + 1);
        if (_falhasRestantes.TryGetValue(evento.PedidoId, out var restantes) && restantes > 0)
        {
            _falhasRestantes[evento.PedidoId] = restantes == int.MaxValue ? restantes : restantes - 1;
            throw new TimeoutException($"Serviço de e-mail fora do ar (pedido {evento.PedidoId}).");
        }
        _processados.Enqueue(evento);
        return Task.CompletedTask;
    }
}
