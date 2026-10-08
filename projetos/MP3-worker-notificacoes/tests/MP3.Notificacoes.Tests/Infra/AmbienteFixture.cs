using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using MP3.Contratos;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;

namespace MP3.Notificacoes.Tests.Infra;

/// <summary>
/// Sobe UMA vez por execução da suíte:
/// <list type="bullet">
/// <item>uma rede Docker própria;</item>
/// <item>um SQL Server 2022 — usado pelo emulador (metadados dele) E pela inbox do worker (banco <c>Mp3Notificacoes</c>);</item>
/// <item>o emulador do Azure Service Bus com a topologia de <c>infra/servicebus/Config.json</c>.</item>
/// </list>
/// Portas aleatórias, nomes gerados: pode rodar em paralelo com outras suítes da máquina.
/// </summary>
public sealed class AmbienteFixture : IAsyncLifetime
{
    public const string ImagemEmulador = "mcr.microsoft.com/azure-messaging/servicebus-emulator:latest";
    public const string ImagemSqlServer = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string BancoDaInbox = "Mp3Notificacoes";
    private const string AliasDoSql = "sql";

    private INetwork? rede;
    private MsSqlContainer? sql;
    private ServiceBusContainer? emulador;

    /// <summary>Connection string do emulador (<c>UseDevelopmentEmulator=true</c>).</summary>
    public string ServiceBus { get; private set; } = "";

    /// <summary>Connection string do banco da inbox (já criado, vazio).</summary>
    public string Inbox { get; private set; } = "";

    /// <summary>
    /// Um cliente para a suíte toda (é a conexão AMQP: cara e thread-safe). O emulador aceita poucas
    /// conexões simultâneas, então não crie um por teste.
    /// </summary>
    public ServiceBusClient Cliente { get; private set; } = null!;

    public TimeSpan TempoDeSubida { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var relogio = Stopwatch.StartNew();
        var senha = $"Mp3-{Guid.NewGuid():N}a1"; // gerada por execução: nunca é segredo de verdade

        rede = new NetworkBuilder().Build();
        sql = new MsSqlBuilder(ImagemSqlServer)
            .WithNetwork(rede)
            .WithNetworkAliases(AliasDoSql)
            .WithPassword(senha)
            .Build();
        emulador = new ServiceBusBuilder(ImagemEmulador)
            .WithAcceptLicenseAgreement(true)
            .WithMsSqlContainer(rede, sql, AliasDoSql, senha)
            .WithConfig(Path.Combine(AppContext.BaseDirectory, "Infra", "servicebus", "Config.json"))
            .Build();

        await emulador.StartAsync(); // sobe o SQL (dependência) e depois o emulador

        ServiceBus = emulador.GetConnectionString();
        Cliente = new ServiceBusClient(ServiceBus);
        Inbox = await CriarBancoDaInboxAsync(sql.GetConnectionString());
        TempoDeSubida = relogio.Elapsed;
    }

    private static async Task<string> CriarBancoDaInboxAsync(string master)
    {
        await using (var conexao = new SqlConnection(master))
        {
            await conexao.OpenAsync();
            await using var comando = new SqlCommand($"IF DB_ID('{BancoDaInbox}') IS NULL CREATE DATABASE [{BancoDaInbox}];", conexao);
            await comando.ExecuteNonQueryAsync();
        }
        return new SqlConnectionStringBuilder(master) { InitialCatalog = BancoDaInbox }.ConnectionString;
    }

    /// <summary>
    /// Esvazia a fila e a DLQ (receive-and-delete). Mensagens AGENDADAS para o futuro não são alcançadas:
    /// por isso os testes sempre filtram pelos próprios ids, nunca por "contagem total da fila".
    /// </summary>
    public async Task DrenarAsync(string fila = ConvencoesDeMensagem.FilaDeNotificacoes)
    {
        foreach (var subFila in new[] { SubQueue.None, SubQueue.DeadLetter })
        {
            await using var receiver = Cliente.CreateReceiver(fila, new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
                SubQueue = subFila,
            });
            while ((await receiver.ReceiveMessagesAsync(100, TimeSpan.FromMilliseconds(300))).Count > 0)
            {
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Cliente is not null) await Cliente.DisposeAsync();
        if (emulador is not null) await emulador.DisposeAsync();
        if (sql is not null) await sql.DisposeAsync();
        if (rede is not null) await rede.DisposeAsync();
    }
}

/// <summary>
/// Testes que usam o ambiente ficam nesta coleção: compartilham os containers e rodam em série
/// (a fila é compartilhada e o emulador foi pensado para uso sequencial).
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoAmbiente : ICollectionFixture<AmbienteFixture>
{
    public const string Nome = "Ambiente (Service Bus + SQL)";
}
