using Dapper;
using Microsoft.Data.SqlClient;

namespace F3M06.Dapper.Leitura;

/// <summary>
/// Consultas de leitura de produtos com Dapper. SQL curto fica em constantes, perto de quem usa.
/// Cada método abre a própria conexão (o pool do ADO.NET torna isso barato) e a fecha no fim.
/// </summary>
public sealed class ProdutoQueries(string connectionString)
{
    // TODO: declare aqui as constantes de SQL (raw string literals """ ... """ ficam ótimas para isso).

    /// <summary>
    /// Passo 1: busca um produto pelo SKU usando PARÂMETRO (<c>@Sku</c>). Devolve <c>null</c> se não existir.
    /// </summary>
    public Task<ProdutoResumo?> ObterPorSkuAsync(string sku, CancellationToken ct = default)
    {
        _ = connectionString;
        throw new NotImplementedException(
            "TODO Passo 1: SELECT Id, Sku, Nome, Preco, Ativo FROM Produtos WHERE Sku = @Sku, com " +
            "conexao.QuerySingleOrDefaultAsync<ProdutoResumo>(new CommandDefinition(sql, new { Sku = sku }, cancellationToken: ct)).");
    }

    /// <summary>
    /// Passo 2: produtos ATIVOS cujo nome contém <paramref name="termo"/>, ordenados por nome.
    /// O termo é tratado como TEXTO: nada de SQL injection, e curingas do LIKE (<c>%</c>, <c>_</c>, <c>[</c>)
    /// digitados pelo usuário são procurados literalmente.
    /// </summary>
    public Task<IReadOnlyList<ProdutoResumo>> BuscarPorNomeAsync(string termo, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO Passo 2: WHERE Ativo = 1 AND Nome LIKE @Padrao ORDER BY Nome, com Padrao = \"%\" + termo escapado + \"%\". " +
            "Escape os curingas do LIKE: [ → [[], % → [%], _ → [_]. Nunca concatene o termo no SQL (veja ConsultasInseguras).");

    /// <summary>
    /// Passo 3: produtos cujos ids estão na lista, ordenados por SKU, sem duplicatas.
    /// Lista vazia devolve vazio SEM ir ao banco. Tem que funcionar com milhares de ids
    /// (o SQL Server aceita no máximo 2.100 parâmetros por comando).
    /// </summary>
    public Task<IReadOnlyList<ProdutoResumo>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO Passo 3: lista vazia → []. Depois, UM parâmetro com os ids em JSON e " +
            "WHERE Id IN (SELECT CAST([value] AS uniqueidentifier) FROM OPENJSON(@IdsJson)) ORDER BY Sku. " +
            "\"IN @Ids\" do Dapper cria um parâmetro por id e estoura acima de 2.100.");
}
