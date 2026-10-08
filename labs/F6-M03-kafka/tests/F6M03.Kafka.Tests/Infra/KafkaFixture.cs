using Testcontainers.Kafka;

namespace F6M03.Kafka.Tests.Infra;

/// <summary>
/// Sobe UM broker Kafka (KRaft, sem ZooKeeper) para o assembly de testes inteiro. Pronto: leia, não altere.
/// Cada teste cria tópicos e grupos com nomes únicos, então não há estado vazando entre testes.
/// </summary>
public sealed class KafkaFixture : IAsyncLifetime
{
    /// <summary>Imagem fixa: a mesma no seu PC e no CI.</summary>
    public const string Imagem = "confluentinc/cp-kafka:7.9.0";

    private readonly KafkaContainer _container = new KafkaBuilder(Imagem)
        .WithKRaft()
        // Sem isto o broker espera 3 s antes do primeiro rebalance de cada grupo novo.
        .WithEnvironment("KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
        .Build();

    /// <summary>Endereço para BootstrapServers (porta aleatória no host).</summary>
    public string Bootstrap { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Bootstrap = _container.GetBootstrapAddress();
    }

    /// <summary>Gera um nome único (tópico ou grupo) para o teste.</summary>
    public static string NomeUnico(string prefixo) => $"{prefixo}-{Guid.NewGuid():N}"[..(prefixo.Length + 13)];

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}

/// <summary>Todos os testes que usam o broker compartilham a fixture.</summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoKafka : ICollectionFixture<KafkaFixture>
{
    public const string Nome = "Kafka";
}
