using Microsoft.Data.SqlClient;

namespace MP3.Notificacoes.Worker.Inbox;

/// <summary>
/// Tabela inbox: "este consumidor já tratou este evento?". A chave primária (Consumidor, EventoId) é a trava:
/// dois processamentos do mesmo evento NÃO conseguem inserir a mesma linha.
/// </summary>
public sealed class InboxSql(IConfiguration configuracao, TimeProvider relogio)
{
    public const string NomeDaConnectionString = "Inbox";

    private const int ViolacaoDeChavePrimaria = 2627;
    private const int ViolacaoDeIndiceUnico = 2601;

    public string ConnectionString =>
        configuracao.GetConnectionString(NomeDaConnectionString) is { Length: > 0 } cs
            ? cs
            : throw new InvalidOperationException("ConnectionStrings:Inbox não configurada (use variável de ambiente ou user-secrets).");

    public const string Esquema = """
        IF SCHEMA_ID('inbox') IS NULL EXEC('CREATE SCHEMA inbox');
        IF OBJECT_ID('inbox.MensagensProcessadas') IS NULL
        BEGIN
            CREATE TABLE inbox.MensagensProcessadas (
                Consumidor   varchar(100)      NOT NULL,
                EventoId     uniqueidentifier  NOT NULL,
                PedidoId     uniqueidentifier  NOT NULL,
                Canal        varchar(20)       NOT NULL,
                ProcessadaEm datetimeoffset(3) NOT NULL,
                CONSTRAINT PK_MensagensProcessadas PRIMARY KEY (Consumidor, EventoId)
            );
            CREATE INDEX IX_MensagensProcessadas_ProcessadaEm ON inbox.MensagensProcessadas (ProcessadaEm); -- limpeza por data
        END
        """;

    public async Task<SqlConnection> AbrirAsync(CancellationToken ct)
    {
        var conexao = new SqlConnection(ConnectionString);
        try
        {
            await conexao.OpenAsync(ct);
            return conexao;
        }
        catch
        {
            await conexao.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Insere a linha do evento DENTRO da transação do chamador. Devolve <c>false</c> se o evento já foi processado.
    /// Se outra transação inseriu a mesma chave e ainda não terminou, este INSERT ESPERA (lock da chave):
    /// commit dela → violação de PK → <c>false</c>; rollback dela → o INSERT passa e este processamento assume.
    /// </summary>
    public async Task<bool> TentarRegistrarAsync(
        SqlConnection conexao, SqlTransaction transacao, string consumidor, Guid eventoId, Guid pedidoId, string canal, CancellationToken ct)
    {
        await using var comando = new SqlCommand("""
            INSERT INTO inbox.MensagensProcessadas (Consumidor, EventoId, PedidoId, Canal, ProcessadaEm)
            VALUES (@consumidor, @eventoId, @pedidoId, @canal, @agora);
            """, conexao, transacao);
        comando.Parameters.AddWithValue("@consumidor", consumidor);
        comando.Parameters.AddWithValue("@eventoId", eventoId);
        comando.Parameters.AddWithValue("@pedidoId", pedidoId);
        comando.Parameters.AddWithValue("@canal", canal);
        comando.Parameters.AddWithValue("@agora", relogio.GetUtcNow());
        try
        {
            await comando.ExecuteNonQueryAsync(ct);
            return true;
        }
        catch (SqlException ex) when (ex.Number is ViolacaoDeChavePrimaria or ViolacaoDeIndiceUnico)
        {
            return false;
        }
    }

    public async Task CriarEsquemaAsync(CancellationToken ct)
    {
        await using var conexao = await AbrirAsync(ct);
        await using var comando = new SqlCommand(Esquema, conexao);
        await comando.ExecuteNonQueryAsync(ct);
    }
}

/// <summary>
/// Garante a tabela da inbox na subida. Num sistema maior, isto seria uma migration (Fase 3);
/// aqui o script é idempotente e roda antes do consumidor começar (hosted services sobem em ordem).
/// </summary>
public sealed class InicializadorDaInbox(InboxSql inbox) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => inbox.CriarEsquemaAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
