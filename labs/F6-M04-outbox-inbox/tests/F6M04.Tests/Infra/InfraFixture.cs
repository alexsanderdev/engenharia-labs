using F6M04.Notificacoes.Infra;
using F6M04.Pedidos.Infra;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Testcontainers.RabbitMq;

namespace F6M04.Tests.Infra;

/// <summary>
/// PRONTA. Sobe UM SQL Server e UM RabbitMQ para a assembly de testes (em paralelo) e cria dois bancos:
/// <c>F6M04_Pedidos</c> (agregado + Outbox) e <c>F6M04_Notificacoes</c> (efeito + Inbox) — serviços diferentes,
/// bancos diferentes, nenhuma transação distribuída entre eles.
/// </summary>
public sealed class InfraFixture : IAsyncLifetime
{
    public const string ImagemSqlServer = "mcr.microsoft.com/mssql/server:2022-latest";
    public const string ImagemRabbitMq = "rabbitmq:4.1-management";

    private MsSqlContainer? _sql;
    private RabbitMqContainer? _rabbit;
    private int _sequencial;

    public string PedidosCs { get; private set; } = "";

    public string NotificacoesCs { get; private set; } = "";

    /// <summary>URI AMQP do broker do container (porta aleatória).</summary>
    public string AmqpUri { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        _sql = new MsSqlBuilder(ImagemSqlServer).Build();
        _rabbit = new RabbitMqBuilder(ImagemRabbitMq).Build();
        await Task.WhenAll(_sql.StartAsync(), _rabbit.StartAsync());

        AmqpUri = _rabbit.GetConnectionString();
        PedidosCs = Montar(_sql.GetConnectionString(), "F6M04_Pedidos");
        NotificacoesCs = Montar(_sql.GetConnectionString(), "F6M04_Notificacoes");

        await using (var pedidos = new PedidosDbContext(new DbContextOptionsBuilder<PedidosDbContext>().UseSqlServer(PedidosCs).Options))
            await pedidos.Database.EnsureCreatedAsync();

        await using (var notificacoes = new NotificacoesDbContext(new DbContextOptionsBuilder<NotificacoesDbContext>().UseSqlServer(NotificacoesCs).Options))
            await notificacoes.Database.EnsureCreatedAsync();
    }

    /// <summary>Número único por teste (nomes de exchange/fila não colidem entre testes).</summary>
    public int Proximo() => Interlocked.Increment(ref _sequencial);

    /// <summary>Antes de cada teste: apaga os dados dos dois bancos (o schema fica).</summary>
    public async Task ResetarBancosAsync()
    {
        await ExecutarAsync(PedidosCs, "DELETE FROM dbo.OutboxMessages; DELETE FROM dbo.Pedidos;");
        await ExecutarAsync(NotificacoesCs, "DELETE FROM dbo.InboxMessages; DELETE FROM dbo.Notificacoes;");
    }

    private static string Montar(string baseCs, string banco) =>
        new SqlConnectionStringBuilder(baseCs) { InitialCatalog = banco, ConnectTimeout = 15 }.ConnectionString;

    private static async Task ExecutarAsync(string cs, string sql)
    {
        await using var conexao = new SqlConnection(cs);
        await conexao.OpenAsync();
        await using var comando = new SqlCommand(sql, conexao);
        await comando.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_sql is not null) await _sql.DisposeAsync();
        if (_rabbit is not null) await _rabbit.DisposeAsync();
    }
}

/// <summary>
/// Todos os testes compartilham os containers e rodam em SÉRIE: os testes de concorrência criam a concorrência
/// que querem, de forma controlada; um vizinho rodando em paralelo seria concorrência NÃO controlada.
/// </summary>
[CollectionDefinition(Nome)]
public sealed class ColecaoInfra : ICollectionFixture<InfraFixture>
{
    public const string Nome = "SQL Server + RabbitMQ";
}
