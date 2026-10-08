using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;

namespace F3M06.Dapper.Leitura;

/// <summary>
/// Consultas de leitura de produtos com Dapper. SQL curto fica em constantes, perto de quem usa.
/// Cada método abre a própria conexão (o pool do ADO.NET torna isso barato) e a fecha no fim.
/// </summary>
public sealed class ProdutoQueries(string connectionString)
{
    private const string SqlPorSku = """
        SELECT Id, Sku, Nome, Preco, Ativo
        FROM Produtos
        WHERE Sku = @Sku
        """;

    private const string SqlBuscaPorNome = """
        SELECT Id, Sku, Nome, Preco, Ativo
        FROM Produtos
        WHERE Ativo = 1 AND Nome LIKE @Padrao
        ORDER BY Nome
        """;

    // OPENJSON transforma UM parâmetro (texto JSON) em uma tabela: 1 parâmetro, qualquer quantidade de ids,
    // e o MESMO texto de SQL (um plano no cache) para listas de qualquer tamanho.
    private const string SqlPorIds = """
        SELECT p.Id, p.Sku, p.Nome, p.Preco, p.Ativo
        FROM Produtos AS p
        WHERE p.Id IN (SELECT CAST(j.[value] AS uniqueidentifier) FROM OPENJSON(@IdsJson) AS j)
        ORDER BY p.Sku
        """;

    /// <summary>
    /// Passo 1: busca um produto pelo SKU usando PARÂMETRO (<c>@Sku</c>). Devolve <c>null</c> se não existir.
    /// </summary>
    public async Task<ProdutoResumo?> ObterPorSkuAsync(string sku, CancellationToken ct = default)
    {
        await using var conexao = new SqlConnection(connectionString);
        var comando = new CommandDefinition(SqlPorSku, new { Sku = sku }, cancellationToken: ct);
        return await conexao.QuerySingleOrDefaultAsync<ProdutoResumo>(comando);
    }

    /// <summary>
    /// Passo 2: produtos ATIVOS cujo nome contém <paramref name="termo"/>, ordenados por nome.
    /// O termo é tratado como TEXTO: nada de SQL injection, e curingas do LIKE (<c>%</c>, <c>_</c>, <c>[</c>)
    /// digitados pelo usuário são procurados literalmente.
    /// </summary>
    public async Task<IReadOnlyList<ProdutoResumo>> BuscarPorNomeAsync(string termo, CancellationToken ct = default)
    {
        var padrao = $"%{EscaparLike(termo)}%";

        await using var conexao = new SqlConnection(connectionString);
        var comando = new CommandDefinition(SqlBuscaPorNome, new { Padrao = padrao }, cancellationToken: ct);
        var produtos = await conexao.QueryAsync<ProdutoResumo>(comando);
        return produtos.AsList();
    }

    /// <summary>
    /// Passo 3: produtos cujos ids estão na lista, ordenados por SKU, sem duplicatas.
    /// Lista vazia devolve vazio SEM ir ao banco. Tem que funcionar com milhares de ids
    /// (o SQL Server aceita no máximo 2.100 parâmetros por comando).
    /// </summary>
    public async Task<IReadOnlyList<ProdutoResumo>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return [];

        var idsJson = JsonSerializer.Serialize(ids.Distinct());

        await using var conexao = new SqlConnection(connectionString);
        var comando = new CommandDefinition(SqlPorIds, new { IdsJson = idsJson }, cancellationToken: ct);
        var produtos = await conexao.QueryAsync<ProdutoResumo>(comando);
        return produtos.AsList();
    }

    /// <summary>No LIKE do SQL Server, colchetes "escapam" um caractere: [%] casa com o próprio %.</summary>
    private static string EscaparLike(string termo) =>
        termo.Replace("[", "[[]", StringComparison.Ordinal)
             .Replace("%", "[%]", StringComparison.Ordinal)
             .Replace("_", "[_]", StringComparison.Ordinal);
}
