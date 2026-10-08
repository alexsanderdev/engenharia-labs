using System.Data;
using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Estoque;

/// <summary>Linha do relatório: estoque de um produto.</summary>
public sealed record ItemDeEstoque(int ProdutoId, string Sku, int Estoque);

/// <summary>
/// Relatório de estoque com duas partes que PRECISAM bater entre si:
/// o total (<see cref="SomaDoEstoque"/>) e o detalhe por produto (<see cref="Itens"/>).
/// </summary>
public sealed record RelatorioDeEstoque(int SomaDoEstoque, IReadOnlyList<ItemDeEstoque> Itens)
{
    /// <summary>O total bate com a soma das linhas?</summary>
    public bool Consistente => SomaDoEstoque == Itens.Sum(i => i.Estoque);
}

/// <summary>
/// Gera o relatório com DUAS consultas (o total e o detalhe), como acontece em relatórios reais
/// (cabeçalho + itens, resumo + gráfico, várias abas...).
/// </summary>
public sealed class GeradorDeRelatorio(string connectionString)
{
    /// <summary>
    /// Gera o relatório de forma consistente: as duas consultas precisam enxergar o MESMO
    /// estado do banco, mesmo que alguém grave entre elas, e SEM bloquear quem grava.
    /// Chame <paramref name="entreAsConsultas"/> entre a consulta do total e a do detalhe.
    /// </summary>
    public async Task<RelatorioDeEstoque> GerarAsync(Func<Task>? entreAsConsultas = null, CancellationToken ct = default)
    {
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync(ct);

        // SNAPSHOT: todas as leituras da transação veem a foto do banco tirada no primeiro acesso a
        // dados, lendo versões do version store (tempdb). Leitor não pede S lock, então não
        // bloqueia o escritor e não é bloqueado por ele. Exige ALLOW_SNAPSHOT_ISOLATION ON.
        await using var transacao = (SqlTransaction)await conexao.BeginTransactionAsync(IsolationLevel.Snapshot, ct);

        int soma;
        await using (var total = new SqlCommand("SELECT SUM(Estoque) FROM dbo.Produtos;", conexao, transacao))
            soma = (int)(await total.ExecuteScalarAsync(ct))!;

        if (entreAsConsultas is not null) await entreAsConsultas();

        var itens = new List<ItemDeEstoque>();
        await using (var detalhe = new SqlCommand("SELECT Id, Sku, Estoque FROM dbo.Produtos ORDER BY Id;", conexao, transacao))
        await using (var leitor = await detalhe.ExecuteReaderAsync(ct))
        {
            while (await leitor.ReadAsync(ct))
                itens.Add(new ItemDeEstoque(leitor.GetInt32(0), leitor.GetString(1), leitor.GetInt32(2)));
        }

        await transacao.CommitAsync(ct);
        return new RelatorioDeEstoque(soma, itens);
    }
}
