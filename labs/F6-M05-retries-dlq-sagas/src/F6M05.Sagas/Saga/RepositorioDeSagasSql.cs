using System.Text.Json;
using Dapper;
using F6M05.Sagas.Mensageria;
using Microsoft.Data.SqlClient;

namespace F6M05.Sagas.Saga;

/// <summary>
/// Estado da saga no SQL Server (Dapper) com concorrência otimista por coluna <c>Versao</c>.
/// </summary>
/// <remarks>
/// O <c>UPDATE ... WHERE PedidoId = @PedidoId AND Versao = @Versao</c> é o coração: se outra
/// instância gravou depois da nossa leitura, a versão no banco mudou, o UPDATE afeta 0 linhas e
/// NADA é gravado. Sem lock segurado entre leitura e escrita (por isso "otimista").
/// </remarks>
public sealed class RepositorioDeSagasSql(string connectionString) : IRepositorioDeSagas
{
    private const int ViolacaoDePk = 2627;
    private const int ViolacaoDeIndiceUnico = 2601;

    private sealed record Linha(
        Guid PedidoId, Guid ClienteId, decimal Valor, string ItensJson, byte Status, int Passos,
        string? AutorizacaoId, string? MotivoCancelamento, DateTimeOffset? PrazoPagamentoEm,
        DateTimeOffset CriadaEm, DateTimeOffset AtualizadaEm, int Versao);

    public async Task<SagaPedido?> ObterAsync(Guid pedidoId, CancellationToken ct = default)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);

        using var multi = await conexao.QueryMultipleAsync(new CommandDefinition(
            """
            SELECT PedidoId, ClienteId, Valor, ItensJson, Status, Passos, AutorizacaoId, MotivoCancelamento,
                   PrazoPagamentoEm, CriadaEm, AtualizadaEm, Versao
              FROM dbo.SagaPedido WHERE PedidoId = @pedidoId;
            SELECT MessageId FROM dbo.SagaPedidoMensagem WHERE PedidoId = @pedidoId;
            """,
            new { pedidoId }, cancellationToken: ct));

        var linha = await multi.ReadSingleOrDefaultAsync<Linha>();
        if (linha is null) return null;
        var mensagens = await multi.ReadAsync<string>();

        return SagaPedido.Reconstituir(
            linha.PedidoId, linha.ClienteId, linha.Valor,
            JsonSerializer.Deserialize<List<ItemDoPedido>>(linha.ItensJson, Serializador.Opcoes) ?? [],
            (StatusSaga)linha.Status, (PassosDaSaga)linha.Passos, linha.AutorizacaoId, linha.MotivoCancelamento,
            linha.PrazoPagamentoEm, linha.CriadaEm, linha.AtualizadaEm, linha.Versao, mensagens);
    }

    public async Task InserirAsync(SagaPedido saga, string messageId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(saga);
        if (saga.Versao != 0)
            throw new InvalidOperationException("Só sagas novas (versão 0) podem ser inseridas.");

        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(ct);
        try
        {
            await conexao.ExecuteAsync(new CommandDefinition(
                """
                INSERT dbo.SagaPedido (PedidoId, ClienteId, Valor, ItensJson, Status, Passos, AutorizacaoId,
                                       MotivoCancelamento, PrazoPagamentoEm, CriadaEm, AtualizadaEm, Versao)
                VALUES (@PedidoId, @ClienteId, @Valor, @ItensJson, @Status, @Passos, @AutorizacaoId,
                        @MotivoCancelamento, @PrazoPagamentoEm, @CriadaEm, @AtualizadaEm, 1);
                """,
                Parametros(saga), transacao, cancellationToken: ct));

            await RegistrarMensagemAsync(conexao, transacao, saga, messageId, ct);
            await transacao.CommitAsync(ct);
        }
        catch (SqlException ex) when (ex.Number is ViolacaoDePk or ViolacaoDeIndiceUnico)
        {
            await transacao.RollbackAsync(CancellationToken.None);
            throw new ConflitoDeConcorrenciaException(saga.PedidoId, 0);
        }

        saga.Versao = 1;
    }

    public async Task AtualizarAsync(SagaPedido saga, string messageId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(saga);

        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(ct);
        try
        {
            var afetadas = await conexao.ExecuteAsync(new CommandDefinition(
                """
                UPDATE dbo.SagaPedido
                   SET Status = @Status, Passos = @Passos, AutorizacaoId = @AutorizacaoId,
                       MotivoCancelamento = @MotivoCancelamento, PrazoPagamentoEm = @PrazoPagamentoEm,
                       AtualizadaEm = @AtualizadaEm, Versao = Versao + 1
                 WHERE PedidoId = @PedidoId AND Versao = @Versao;
                """,
                Parametros(saga), transacao, cancellationToken: ct));

            if (afetadas == 0)
            {
                await transacao.RollbackAsync(CancellationToken.None);
                throw new ConflitoDeConcorrenciaException(saga.PedidoId, saga.Versao);
            }

            await RegistrarMensagemAsync(conexao, transacao, saga, messageId, ct);
            await transacao.CommitAsync(ct);
        }
        catch (SqlException ex) when (ex.Number is ViolacaoDePk or ViolacaoDeIndiceUnico)
        {
            await transacao.RollbackAsync(CancellationToken.None);
            throw new ConflitoDeConcorrenciaException(saga.PedidoId, saga.Versao);
        }

        saga.Versao++;
    }

    public async Task<IReadOnlyList<Guid>> ListarComPrazoVencidoAsync(DateTimeOffset agora, int maximo = 100, CancellationToken ct = default)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        var ids = await conexao.QueryAsync<Guid>(new CommandDefinition(
            """
            SELECT TOP (@maximo) PedidoId
              FROM dbo.SagaPedido
             WHERE Status = @status AND PrazoPagamentoEm IS NOT NULL AND PrazoPagamentoEm <= @agora
             ORDER BY PrazoPagamentoEm;
            """,
            new { maximo, status = (byte)StatusSaga.AguardandoPagamento, agora }, cancellationToken: ct));
        return ids.AsList();
    }

    private static Task<int> RegistrarMensagemAsync(SqlConnection conexao, SqlTransaction transacao, SagaPedido saga, string messageId, CancellationToken ct) =>
        conexao.ExecuteAsync(new CommandDefinition(
            "INSERT dbo.SagaPedidoMensagem (PedidoId, MessageId, ProcessadaEm) VALUES (@PedidoId, @MessageId, @AtualizadaEm);",
            new { saga.PedidoId, MessageId = messageId, saga.AtualizadaEm }, transacao, cancellationToken: ct));

    private static object Parametros(SagaPedido saga) => new
    {
        saga.PedidoId,
        saga.ClienteId,
        saga.Valor,
        ItensJson = JsonSerializer.Serialize(saga.Itens, Serializador.Opcoes),
        Status = (byte)saga.Status,
        Passos = (int)saga.Passos,
        saga.AutorizacaoId,
        saga.MotivoCancelamento,
        saga.PrazoPagamentoEm,
        saga.CriadaEm,
        saga.AtualizadaEm,
        saga.Versao,
    };
}
