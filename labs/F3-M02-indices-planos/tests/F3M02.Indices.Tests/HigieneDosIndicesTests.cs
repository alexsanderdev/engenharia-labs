using F3M02.Indices.Tests.Infra;

namespace F3M02.Indices.Tests;

/// <summary>
/// Passo 7 — Índice não é de graça: cada um é atualizado em todo INSERT/UPDATE/DELETE.
/// Poucos índices, cada um com um motivo, e nenhum que seja prefixo de outro.
/// </summary>
public sealed class HigieneDosIndicesTests(SqlServerFixture fixture) : PlanoTestBase(fixture)
{
    [Fact]
    public async Task Indices_PoucosEUteis_EntreUmEQuatroEmPedidosENenhumRedundante()
    {
        var linhas = await ConsultarAsync("""
            SELECT OBJECT_NAME(i.object_id) AS Tabela, i.name, i.is_unique, i.has_filter,
                   STRING_AGG(COL_NAME(ic.object_id, ic.column_id), ',') WITHIN GROUP (ORDER BY ic.key_ordinal) AS Chave
            FROM sys.indexes AS i
            JOIN sys.index_columns AS ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.key_ordinal > 0
            WHERE i.type = 2 AND OBJECTPROPERTY(i.object_id, 'IsUserTable') = 1
            GROUP BY i.object_id, i.name, i.is_unique, i.has_filter
            """);
        var indices = linhas
            .Select(l => (Tabela: (string)l[0]!, Nome: (string)l[1]!, Unico: (bool)l[2]!, Filtrado: (bool)l[3]!, Chave: (string)l[4]! + ","))
            .ToList();

        indices.Count(i => i.Tabela == "Pedidos").ShouldBeInRange(1, 4,
            $"Não clusterizados em Pedidos: [{string.Join(", ", indices.Where(i => i.Tabela == "Pedidos").Select(i => i.Nome))}]");

        // Redundante: índice comum (não único, sem filtro) cuja chave é PREFIXO da chave de outro na mesma tabela.
        var redundantes =
            from a in indices
            from b in indices
            where a.Tabela == b.Tabela && a.Nome != b.Nome && !a.Unico && !a.Filtrado && !b.Filtrado
               && b.Chave.StartsWith(a.Chave, StringComparison.OrdinalIgnoreCase)
            select $"{a.Tabela}.{a.Nome} ({a.Chave.TrimEnd(',')}) é coberto por {b.Nome} ({b.Chave.TrimEnd(',')})";
        redundantes.ShouldBeEmpty();
    }
}
