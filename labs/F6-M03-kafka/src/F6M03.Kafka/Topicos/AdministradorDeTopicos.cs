using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace F6M03.Kafka.Topicos;

/// <summary>
/// Cria e inspeciona tópicos com o <see cref="IAdminClient"/>. Em produção, tópicos nascem por
/// infraestrutura como código (Terraform, Bicep, Strimzi, scripts de pipeline) — nunca por
/// "auto.create.topics" com padrões do broker. Aqui o lab cria por código para ficar visível.
/// </summary>
public sealed class AdministradorDeTopicos(string bootstrapServers) : IDisposable
{
    private static readonly TimeSpan TempoLimite = TimeSpan.FromSeconds(10);

    private readonly IAdminClient _admin =
        new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

    /// <summary>Nome do tópico de retry de <paramref name="topico"/> (ex.: "pedidos.retry").</summary>
    public static string NomeRetry(string topico) => $"{topico}.retry";

    /// <summary>Nome do tópico de dead-letter de <paramref name="topico"/> (ex.: "pedidos.dlq").</summary>
    public static string NomeDlq(string topico) => $"{topico}.dlq";

    /// <summary>
    /// Passo 2: cria o tópico com <paramref name="particoes"/> partições e fator de replicação 1
    /// (o container tem UM broker; em produção use 3 + <c>min.insync.replicas=2</c>).
    /// Deve ser idempotente: se o tópico já existe, não lança (trate <see cref="CreateTopicsException"/>
    /// cujo resultado tem <see cref="ErrorCode.TopicAlreadyExists"/>).
    /// No fim, chame <see cref="AguardarMetadadosAsync"/> (pronto): o controller confirma a criação antes de
    /// todos os brokers enxergarem o tópico.
    /// </summary>
    public async Task CriarTopicoAsync(string nome, int particoes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(particoes, 1);
        try
        {
            await _admin.CreateTopicsAsync(
            [
                new TopicSpecification { Name = nome, NumPartitions = particoes, ReplicationFactor = 1 },
            ]);
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // Já existe: criar tópico é uma operação idempotente para nós.
        }

        await AguardarMetadadosAsync(nome);
    }

    /// <summary>
    /// Passo 2: cria a "topologia" de um fluxo: o tópico principal, o de retry (<see cref="NomeRetry"/>)
    /// e o de DLQ (<see cref="NomeDlq"/>), todos com <paramref name="particoes"/> partições.
    /// </summary>
    public async Task CriarTopologiaAsync(string topico, int particoes)
    {
        await CriarTopicoAsync(topico, particoes);
        await CriarTopicoAsync(NomeRetry(topico), particoes);
        await CriarTopicoAsync(NomeDlq(topico), particoes);
    }

    /// <summary>
    /// Passo 2: devolve quantas partições o tópico tem, lendo os metadados do cluster
    /// (<see cref="IAdminClient.GetMetadata(string, TimeSpan)"/>). Tópico inexistente → 0.
    /// </summary>
    public int ContarParticoes(string topico)
    {
        var metadados = _admin.GetMetadata(topico, TempoLimite);
        var t = metadados.Topics.SingleOrDefault(x => x.Topic == topico);
        return t is null || t.Error.IsError ? 0 : t.Partitions.Count;
    }

    /// <summary>
    /// Pronto: espera (polling com timeout) até os metadados do cluster mostrarem o tópico com partições.
    /// </summary>
    private async Task AguardarMetadadosAsync(string nome)
    {
        var limite = DateTime.UtcNow + TempoLimite;
        while (ParticoesNosMetadados(nome) == 0)
        {
            if (DateTime.UtcNow > limite)
                throw new TimeoutException($"O tópico '{nome}' não apareceu nos metadados em {TempoLimite.TotalSeconds} s.");
            await Task.Delay(50);
        }
    }

    private int ParticoesNosMetadados(string nome)
    {
        try
        {
            var t = _admin.GetMetadata(nome, TempoLimite).Topics.SingleOrDefault(x => x.Topic == nome);
            return t is null || t.Error.IsError ? 0 : t.Partitions.Count;
        }
        catch (KafkaException)
        {
            return 0;
        }
    }

    public void Dispose() => _admin.Dispose();
}
