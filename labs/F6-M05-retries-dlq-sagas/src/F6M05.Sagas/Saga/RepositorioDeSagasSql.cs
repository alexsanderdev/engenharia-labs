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

    /// <summary>PRONTO. Carrega a saga e os MessageIds já processados (duas consultas, uma ida ao banco).</summary>
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

    /// <remarks>
    /// TODO (Passo 6): abra conexão e transação; <c>INSERT dbo.SagaPedido (...) VALUES (..., Versao = 1)</c> com
    /// <see cref="Parametros"/>; <see cref="RegistrarMensagemAsync"/> na MESMA transação; commit; <c>saga.Versao = 1</c>.
    /// <c>SqlException</c> 2627/2601 (PK duplicada: outra instância criou antes) → rollback e
    /// <see cref="ConflitoDeConcorrenciaException"/>. Saga com versão ≠ 0 → <see cref="InvalidOperationException"/>.
    /// </remarks>
    public Task InserirAsync(SagaPedido saga, string messageId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO: insira a saga (versão 1) e a mensagem na mesma transação (Passo 6).");

    /// <remarks>
    /// TODO (Passo 6): na mesma transação,
    /// <c>UPDATE dbo.SagaPedido SET ..., Versao = Versao + 1 WHERE PedidoId = @PedidoId AND Versao = @Versao</c>.
    /// 0 linhas afetadas → rollback e <see cref="ConflitoDeConcorrenciaException"/> (versão esperada = <c>saga.Versao</c>).
    /// Depois <see cref="RegistrarMensagemAsync"/> (PK duplicada → rollback e conflito), commit e <c>saga.Versao++</c>.
    /// </remarks>
    public Task AtualizarAsync(SagaPedido saga, string messageId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO: UPDATE com WHERE Versao = @Versao (concorrência otimista) + mensagem na mesma transação (Passo 6).");

    /// <remarks>
    /// TODO (Passo 6): <c>SELECT TOP (@maximo) PedidoId ... WHERE Status = AguardandoPagamento AND PrazoPagamentoEm &lt;= @agora
    /// ORDER BY PrazoPagamentoEm</c> (o índice filtrado IX_SagaPedido_PrazoPagamento existe para isso).
    /// </remarks>
    public Task<IReadOnlyList<Guid>> ListarComPrazoVencidoAsync(DateTimeOffset agora, int maximo = 100, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO: liste as sagas aguardando pagamento com prazo vencido (Passo 6).");

    /// <summary>PRONTO. Registra a mensagem como processada (a PK impede duplicar).</summary>
    private static Task<int> RegistrarMensagemAsync(SqlConnection conexao, SqlTransaction transacao, SagaPedido saga, string messageId, CancellationToken ct) =>
        conexao.ExecuteAsync(new CommandDefinition(
            "INSERT dbo.SagaPedidoMensagem (PedidoId, MessageId, ProcessadaEm) VALUES (@PedidoId, @MessageId, @AtualizadaEm);",
            new { saga.PedidoId, MessageId = messageId, saga.AtualizadaEm }, transacao, cancellationToken: ct));

    /// <summary>PRONTO. Parâmetros Dapper da saga (Status como tinyint, Passos como int, itens em JSON).</summary>
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
