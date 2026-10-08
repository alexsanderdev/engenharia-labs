using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using F6M02.ServiceBus.Consumo;
using F6M02.ServiceBus.Topologia;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;

namespace F6M02.ServiceBus.Tests.Infra;

/// <summary>
/// Sobe UMA vez por execução o emulador do Azure Service Bus (e o SQL Server de que ele depende),
/// numa rede Docker própria, com a topologia de <c>Topologia/Config.json</c> (do projeto src).
/// Pronta: o foco do lab é o SDK, não o Testcontainers.
/// </summary>
public sealed class ServiceBusFixture : IAsyncLifetime
{
    /// <summary>Imagem do emulador (a mesma em todas as máquinas).</summary>
    public const string ImagemEmulador = "mcr.microsoft.com/azure-messaging/servicebus-emulator:latest";

    /// <summary>O emulador guarda metadados num SQL Server: usamos a imagem 2022 que o curso já baixou.</summary>
    public const string ImagemSqlServer = "mcr.microsoft.com/mssql/server:2022-latest";

    private const string AliasDoSql = "sqlsb";

    private INetwork? _rede;
    private MsSqlContainer? _sql;
    private ServiceBusContainer? _emulador;

    /// <summary>Connection string do emulador (<c>UseDevelopmentEmulator=true</c>).</summary>
    public string ConnectionString { get; private set; } = "";

    /// <summary>
    /// UM <see cref="ServiceBusClient"/> para a suíte toda: ele é a conexão AMQP, caro de abrir e thread-safe
    /// (e o emulador aceita no máximo 10 conexões simultâneas).
    /// </summary>
    public ServiceBusClient Cliente { get; private set; } = null!;

    /// <summary>Tempo que o container levou para subir (aparece na saída dos testes).</summary>
    public TimeSpan TempoDeSubida { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var relogio = Stopwatch.StartNew();
        var senhaDoSql = $"Lab-{Guid.NewGuid():N}A1"; // gerada por execução: nunca é segredo de verdade

        _rede = new NetworkBuilder().Build();

        _sql = new MsSqlBuilder(ImagemSqlServer)
            .WithNetwork(_rede)
            .WithNetworkAliases(AliasDoSql)
            .WithPassword(senhaDoSql)
            .Build();

        _emulador = new ServiceBusBuilder(ImagemEmulador)
            .WithAcceptLicenseAgreement(true)
            .WithMsSqlContainer(_rede, _sql, AliasDoSql, senhaDoSql)
            .WithConfig(CaminhoDaTopologia())
            .Build();

        await _emulador.StartAsync(); // sobe o SQL (dependência) e depois o emulador
        ConnectionString = _emulador.GetConnectionString();
        Cliente = new ServiceBusClient(ConnectionString);
        TempoDeSubida = relogio.Elapsed;
    }

    private static string CaminhoDaTopologia()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "Topologia", "Config.json");
        if (!File.Exists(caminho))
            throw new FileNotFoundException(
                "Topologia/Config.json não foi copiado para a saída dos testes. Confira o <None ... CopyToOutputDirectory> do csproj do src.",
                caminho);
        return caminho;
    }

    /// <summary>
    /// Esvazia a entidade (e a DLQ dela) em receive-and-delete. Cada teste chama no início para não herdar
    /// mensagens de um teste anterior que falhou no meio.
    /// </summary>
    public async Task DrenarAsync(params OrigemDasMensagens[] origens)
    {
        foreach (var origem in origens)
        {
            foreach (var subFila in new[] { SubQueue.None, SubQueue.DeadLetter })
            {
                // Sempre "await using": um receiver esquecido aberto em receive-and-delete continua
                // puxando mensagens (e elas somem!). Foi um bug real durante a produção deste lab.
                await using var receiver = origem.CriarReceiver(Cliente, new ServiceBusReceiverOptions
                {
                    ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
                    SubQueue = subFila,
                });
                while ((await receiver.ReceiveMessagesAsync(100, TimeSpan.FromMilliseconds(300))).Count > 0)
                {
                }
            }
        }
    }

    /// <summary>Envia uma mensagem pronta para uma fila (para os testes do consumidor).</summary>
    public async Task EnviarAsync(string fila, params ServiceBusMessage[] mensagens)
    {
        await using var sender = Cliente.CreateSender(fila);
        await sender.SendMessagesAsync(mensagens);
    }

    public async ValueTask DisposeAsync()
    {
        if (Cliente is not null) await Cliente.DisposeAsync();
        if (_emulador is not null) await _emulador.DisposeAsync();
        if (_sql is not null) await _sql.DisposeAsync();
        if (_rede is not null) await _rede.DisposeAsync();
    }

    /// <summary>Todas as origens usadas pelos testes (para drenar tudo de uma vez se precisar).</summary>
    public static readonly OrigemDasMensagens[] TodasAsOrigens =
    [
        OrigemDasMensagens.DaSubscription(Entidades.TopicoPedidos, Entidades.SubscriptionNotificacao),
        OrigemDasMensagens.DaSubscription(Entidades.TopicoPedidos, Entidades.SubscriptionAntifraude),
        OrigemDasMensagens.DaSubscription(Entidades.TopicoPedidos, Entidades.SubscriptionFidelidade),
        OrigemDasMensagens.Fila(Entidades.FilaProcessamento),
        OrigemDasMensagens.Fila(Entidades.FilaPagamentos),
        OrigemDasMensagens.Fila(Entidades.FilaLembretes),
    ];
}

/// <summary>
/// Testes que usam o broker ficam nesta coleção: compartilham o container e rodam em série
/// (as entidades são compartilhadas, e o próprio emulador é pensado para testes sequenciais).
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoServiceBus : ICollectionFixture<ServiceBusFixture>
{
    public const string Nome = "Service Bus";
}
