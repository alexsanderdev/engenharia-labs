using Dapper;
using Microsoft.Data.SqlClient;

namespace F3M06.Dapper.Leitura;

/// <summary>
/// ⚠️ CONTRAEXEMPLO — NÃO copie. Existe só para o teste provar que concatenar entrada do
/// usuário no SQL abre a porta para SQL injection. Usar Dapper NÃO protege nada se você
/// montar a string na mão: quem protege é o PARÂMETRO.
/// </summary>
public sealed class ConsultasInseguras(string connectionString)
{
    public async Task<IReadOnlyList<ProdutoResumo>> BuscarProdutosPorNomeConcatenandoAsync(string termo)
    {
        // A entrada do usuário vira CÓDIGO SQL. Um apóstrofo no termo fecha a string e o resto é executado.
        var sql = "SELECT Id, Sku, Nome, Preco, Ativo FROM Produtos " +
                  "WHERE Ativo = 1 AND Nome LIKE '%" + termo + "%' ORDER BY Nome";

        await using var conexao = new SqlConnection(connectionString);
        var produtos = await conexao.QueryAsync<ProdutoResumo>(sql);
        return produtos.AsList();
    }
}
