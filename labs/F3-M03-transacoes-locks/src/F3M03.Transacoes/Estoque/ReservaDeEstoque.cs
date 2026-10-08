using System.Data;
using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Estoque;

/// <summary>
/// Baixa de estoque na tabela <c>dbo.Produtos</c> (colunas <c>Id</c>, <c>Estoque</c>, <c>Versao rowversion</c>).
/// Quatro versões do MESMO caso de uso: a ingênua (pronta, com lost update) e as três correções.
/// </summary>
/// <remarks>
/// O parâmetro <c>entreLeituraEEscrita</c> existe só para os testes: é um "gancho" chamado entre
/// a leitura do estoque e a escrita, que permite ao teste pausar a transação naquele ponto e
/// intercalar duas execuções de forma determinística. Em produção ele é <c>null</c>.
/// </remarks>
public sealed class ReservaDeEstoque(string connectionString)
{
    /// <summary>
    /// PRONTO (é o material da demonstração): read-modify-write ingênuo.
    /// Lê o estoque, calcula o novo valor em C# e grava o valor calculado.
    /// Mesmo dentro de uma transação READ COMMITTED, duas execuções intercaladas
    /// leem o mesmo valor e a segunda escrita apaga a primeira (lost update).
    /// </summary>
    public async Task<ResultadoReserva> ReservarIngenuoAsync(
        int produtoId, int quantidade, Func<Task>? entreLeituraEEscrita = null, CancellationToken ct = default)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

        await using var ler = new SqlCommand("SELECT Estoque FROM dbo.Produtos WHERE Id = @id;", conexao, transacao);
        ler.Parameters.AddWithValue("@id", produtoId);
        var estoque = (int)(await ler.ExecuteScalarAsync(ct))!;

        if (entreLeituraEEscrita is not null) await entreLeituraEEscrita();

        if (estoque < quantidade)
        {
            await transacao.RollbackAsync(ct);
            return ResultadoReserva.EstoqueInsuficiente;
        }

        // O bug está aqui: grava um valor calculado a partir de uma leitura que pode estar velha.
        await using var gravar = new SqlCommand("UPDATE dbo.Produtos SET Estoque = @novo WHERE Id = @id;", conexao, transacao);
        gravar.Parameters.AddWithValue("@novo", estoque - quantidade);
        gravar.Parameters.AddWithValue("@id", produtoId);
        await gravar.ExecuteNonQueryAsync(ct);

        await transacao.CommitAsync(ct);
        return ResultadoReserva.Reservado;
    }

    /// <summary>
    /// Correção 1 — UPDATE atômico condicional: um único comando que lê e escreve
    /// (<c>SET Estoque = Estoque - @q WHERE Id = @id AND Estoque &gt;= @q</c>).
    /// Linhas afetadas = 1 → <see cref="ResultadoReserva.Reservado"/>; 0 → <see cref="ResultadoReserva.EstoqueInsuficiente"/>.
    /// </summary>
    public Task<ResultadoReserva> ReservarAtomicoAsync(int produtoId, int quantidade, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 1): abra a conexão e execute UM único UPDATE condicional " +
            "(SET Estoque = Estoque - @q WHERE Id = @id AND Estoque >= @q). Não leia antes! " +
            "ExecuteNonQueryAsync devolve as linhas afetadas: 1 = Reservado, 0 = EstoqueInsuficiente.");

    /// <summary>
    /// Correção 2 — lock pessimista: mesma lógica do ingênuo (ler, decidir em C#, gravar), mas a
    /// leitura usa <c>WITH (UPDLOCK, HOLDLOCK)</c> dentro da transação. A segunda transação
    /// fica esperando na LEITURA até a primeira terminar, e então lê o valor já atualizado.
    /// Chame <paramref name="entreLeituraEEscrita"/> entre a leitura e a escrita.
    /// </summary>
    public Task<ResultadoReserva> ReservarComUpdLockAsync(
        int produtoId, int quantidade, Func<Task>? entreLeituraEEscrita = null, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 2): copie o ReservarIngenuoAsync e mude só a leitura para " +
            "SELECT Estoque FROM dbo.Produtos WITH (UPDLOCK, HOLDLOCK) WHERE Id = @id (dentro da transação). " +
            "Mantenha a chamada ao gancho entre a leitura e a escrita.");

    /// <summary>
    /// Correção 3 — concorrência otimista com <c>rowversion</c>: lê <c>Estoque</c> e <c>Versao</c>
    /// SEM lock, chama <paramref name="entreLeituraEEscrita"/>, e grava com
    /// <c>WHERE Id = @id AND Versao = @versaoLida</c>. Se 0 linhas forem afetadas, alguém mudou a
    /// linha: relê e tenta de novo, até <paramref name="maxTentativas"/> tentativas no total
    /// (o gancho é chamado em TODAS as tentativas). Esgotadas as tentativas → <see cref="ResultadoReserva.Conflito"/>.
    /// </summary>
    public Task<ResultadoReserva> ReservarOtimistaAsync(
        int produtoId, int quantidade, int maxTentativas = 3,
        Func<Task>? entreLeituraEEscrita = null, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 3): num laço de 1 a maxTentativas: SELECT Estoque, Versao (sem transação); chame o gancho; " +
            "se faltar estoque devolva EstoqueInsuficiente; senão UPDATE ... SET Estoque = @novo WHERE Id = @id AND Versao = @versao " +
            "(parâmetro SqlDbType.Timestamp). 1 linha = Reservado; 0 = tente de novo. Fim do laço = Conflito.");
}
