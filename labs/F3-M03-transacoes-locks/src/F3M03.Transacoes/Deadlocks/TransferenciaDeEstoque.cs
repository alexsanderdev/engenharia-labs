using System.Data;
using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Deadlocks;

/// <summary>
/// Valor de <c>SET DEADLOCK_PRIORITY</c> da sessão. Em um deadlock, o SQL Server escolhe como
/// vítima a sessão de MENOR prioridade; empatando, a mais barata de desfazer (menos log gerado).
/// </summary>
public enum PrioridadeDeDeadlock
{
    /// <summary>-5: "pode me matar" (jobs de lote, reprocessamentos).</summary>
    Baixa = -5,

    /// <summary>0: padrão.</summary>
    Normal = 0,

    /// <summary>5: transação que não deve ser a vítima (ex.: fechamento de pedido do usuário).</summary>
    Alta = 5,
}

/// <summary>
/// Move estoque entre dois produtos (ex.: transferência entre depósitos modelados como SKUs).
/// Duas atualizações na MESMA transação: tira da origem e põe no destino.
/// </summary>
/// <remarks>
/// O parâmetro <c>entreAsAtualizacoes</c> existe só para os testes: é chamado depois da PRIMEIRA
/// atualização e antes da segunda, com a transação aberta e o primeiro lock X já retido.
/// </remarks>
public sealed class TransferenciaDeEstoque(string connectionString, PrioridadeDeDeadlock prioridade = PrioridadeDeDeadlock.Normal)
{
    /// <summary>
    /// PRONTO (é o material da demonstração): atualiza na ordem "do pedido" — primeiro a origem,
    /// depois o destino. Duas transferências cruzadas (1→2 e 2→1) pegam os locks em ordens
    /// opostas e podem entrar em deadlock.
    /// </summary>
    /// <returns><c>false</c> se a origem não tem estoque suficiente (nada é alterado).</returns>
    public async Task<bool> MoverNaOrdemDoPedidoAsync(
        int origemId, int destinoId, int quantidade, Func<Task>? entreAsAtualizacoes = null, CancellationToken ct = default)
    {
        await using var conexao = await AbrirAsync(ct);
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        if (!await TirarAsync(conexao, transacao, origemId, quantidade, ct))
        {
            await transacao.RollbackAsync(ct);
            return false;
        }

        if (entreAsAtualizacoes is not null) await entreAsAtualizacoes();

        await PorAsync(conexao, transacao, destinoId, quantidade, ct);
        await transacao.CommitAsync(ct);
        return true;
    }

    /// <summary>
    /// Correção do deadlock pela ORDEM DE ACESSO CONSISTENTE: atualiza sempre primeiro o produto
    /// de MENOR Id, qualquer que seja o sentido da transferência. Assim duas transferências
    /// cruzadas disputam primeiro o mesmo lock: uma espera a outra, e não há ciclo.
    /// Chame <paramref name="entreAsAtualizacoes"/> entre a primeira e a segunda atualização.
    /// Se a origem não tiver estoque, desfaça tudo (inclusive o destino, se ele foi atualizado
    /// primeiro) e devolva <c>false</c>.
    /// </summary>
    public Task<bool> MoverEmOrdemConsistenteAsync(
        int origemId, int destinoId, int quantidade, Func<Task>? entreAsAtualizacoes = null, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 6): use AbrirAsync, BeginTransactionAsync e os helpers TirarAsync/PorAsync (como no " +
            "MoverNaOrdemDoPedidoAsync), mas atualize SEMPRE primeiro o produto de MENOR Id. Se a origem vier depois " +
            "e não tiver estoque, faça Rollback (desfaz o destino) e devolva false. Chame o gancho entre as duas atualizações.");

    private async Task<SqlConnection> AbrirAsync(CancellationToken ct)
    {
        var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);

        // Vale para a sessão inteira (até fechar a conexão). ATENÇÃO: sem parâmetros de propósito.
        // Um comando com parâmetros vira sp_executesql, e SET feito dentro de sp_executesql é
        // desfeito quando ele termina. O valor vem de um enum (int), então não há risco de injeção.
        await using var prioridadeCmd = new SqlCommand($"SET DEADLOCK_PRIORITY {(int)prioridade};", conexao);
        await prioridadeCmd.ExecuteNonQueryAsync(ct);
        return conexao;
    }

    private static async Task<bool> TirarAsync(SqlConnection conexao, SqlTransaction transacao, int produtoId, int quantidade, CancellationToken ct)
    {
        await using var comando = new SqlCommand(
            "UPDATE dbo.Produtos SET Estoque = Estoque - @q WHERE Id = @id AND Estoque >= @q;", conexao, transacao);
        comando.Parameters.AddWithValue("@q", quantidade);
        comando.Parameters.AddWithValue("@id", produtoId);
        return await comando.ExecuteNonQueryAsync(ct) == 1;
    }

    private static async Task PorAsync(SqlConnection conexao, SqlTransaction transacao, int produtoId, int quantidade, CancellationToken ct)
    {
        await using var comando = new SqlCommand(
            "UPDATE dbo.Produtos SET Estoque = Estoque + @q WHERE Id = @id;", conexao, transacao);
        comando.Parameters.AddWithValue("@q", quantidade);
        comando.Parameters.AddWithValue("@id", produtoId);
        await comando.ExecuteNonQueryAsync(ct);
    }
}
