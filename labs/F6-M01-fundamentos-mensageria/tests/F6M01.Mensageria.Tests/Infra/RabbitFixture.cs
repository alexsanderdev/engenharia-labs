using F6M01.Mensageria.Topologia;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace F6M01.Mensageria.Tests.Infra;

/// <summary>
/// PRONTA. Sobe UM RabbitMQ em container para a assembly de testes (porta aleatória) e guarda uma
/// conexão compartilhada. Antes de cada teste, <see cref="ResetarAsync"/> apaga as filas e exchanges
/// conhecidas: cada teste começa com o broker "limpo" e declara o que precisa.
/// </summary>
public sealed class RabbitFixture : IAsyncLifetime
{
    public const string Imagem = "rabbitmq:4.1-management";

    /// <summary>Fila temporária que alguns testes usam para observar uma exchange.</summary>
    public const string FilaEspia = "testes.espia";

    private RabbitMqContainer? _container;
    private ConnectionFactory? _fabrica;

    /// <summary>Conexão compartilhada (crie canais à vontade nela).</summary>
    public IConnection Conexao { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _container = new RabbitMqBuilder(Imagem).Build();
        await _container.StartAsync(TestContext.Current.CancellationToken);

        _fabrica = new ConnectionFactory
        {
            Uri = new Uri(_container.GetConnectionString()),
            ClientProvidedName = "F6M01.Tests",
        };
        Conexao = await NovaConexaoAsync("F6M01.Tests.compartilhada");
    }

    /// <summary>
    /// Conexão NOVA e exclusiva — use quando o teste precisa derrubar um consumidor "de verdade"
    /// (<c>AbortAsync</c> na conexão simula o processo morrendo).
    /// </summary>
    public Task<IConnection> NovaConexaoAsync(string nome) =>
        _fabrica!.CreateConnectionAsync(nome, TestContext.Current.CancellationToken);

    /// <summary>Apaga filas e exchanges do lab (delete é idempotente no RabbitMQ).</summary>
    public async Task ResetarAsync()
    {
        await using var canal = await Conexao.CreateChannelAsync();
        foreach (var fila in TopologiaOrderFlow.Filas.Append(FilaEspia))
            await canal.QueueDeleteAsync(fila, ifUnused: false, ifEmpty: false);
        foreach (var exchange in TopologiaOrderFlow.Exchanges)
            await canal.ExchangeDeleteAsync(exchange, ifUnused: false);
    }

    /// <summary>Quantas mensagens estão PRONTAS (ready) na fila — não conta as entregues sem ack.</summary>
    public async Task<uint> ProntasNaFilaAsync(string fila)
    {
        await using var canal = await Conexao.CreateChannelAsync();
        return await canal.MessageCountAsync(fila);
    }

    public async ValueTask DisposeAsync()
    {
        if (Conexao is not null)
            await Conexao.DisposeAsync();
        if (_container is not null)
            await _container.DisposeAsync();
    }
}

/// <summary>
/// Todos os testes com broker ficam nesta coleção: compartilham o container e rodam em SÉRIE
/// (as filas têm nomes fixos, como em produção).
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoRabbit : ICollectionFixture<RabbitFixture>
{
    public const string Nome = "RabbitMQ";
}

/// <summary>Base dos testes com broker: reseta o broker antes de cada teste. (PRONTA)</summary>
[Collection(ColecaoRabbit.Nome)]
public abstract class RabbitTestBase(RabbitFixture rabbit) : IAsyncLifetime
{
    protected RabbitFixture Rabbit { get; } = rabbit;

    protected IConnection Conexao => Rabbit.Conexao;

    public virtual async ValueTask InitializeAsync() => await Rabbit.ResetarAsync();

    public virtual ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>Declara a topologia do lab (o código que VOCÊ escreve em <see cref="TopologiaOrderFlow"/>).</summary>
    protected async Task DeclararTopologiaAsync()
    {
        await using var canal = await Conexao.CreateChannelAsync();
        await TopologiaOrderFlow.DeclararAsync(canal);
    }

    /// <summary>
    /// Lê UMA mensagem da fila sem consumidor (basic.get), esperando até ela chegar (no máximo 5 s).
    /// Confirma automaticamente (autoAck).
    /// </summary>
    protected async Task<BasicGetResult> LerUmaAsync(string fila)
    {
        await using var canal = await Conexao.CreateChannelAsync();
        BasicGetResult? resultado = null;
        await Eventualmente.Ate(async () =>
        {
            resultado = await canal.BasicGetAsync(fila, autoAck: true);
            return resultado is not null;
        }, $"nenhuma mensagem chegou na fila '{fila}'");
        return resultado!;
    }
}
