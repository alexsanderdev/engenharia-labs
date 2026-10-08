
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
    public Task<RelatorioDeEstoque> GerarAsync(Func<Task>? entreAsConsultas = null, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 4): abra a conexão e uma transação; execute SELECT SUM(Estoque) FROM dbo.Produtos, chame o gancho, " +
            "execute SELECT Id, Sku, Estoque FROM dbo.Produtos ORDER BY Id, faça commit e monte o RelatorioDeEstoque. " +
            "Escreva primeiro com READ COMMITTED e veja o teste falhar; depois escolha o nível de isolamento que " +
            $"dá uma foto única SEM bloquear quem grava. (connection string com {connectionString.Length} caracteres)");
}
