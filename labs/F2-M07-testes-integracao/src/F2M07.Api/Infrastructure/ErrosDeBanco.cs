using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace F2M07.Api.Infrastructure;

public static class ErrosDeBanco
{
    /// <summary>
    /// true quando o <see cref="DbUpdateException"/> foi causado por violação de índice/chave única
    /// no SQL Server (erros 2601 e 2627).
    /// </summary>
    public static bool EhViolacaoDeUnicidade(DbUpdateException ex) =>
        throw new NotImplementedException(
            "TODO (Passo 5): devolva true se ex.InnerException for SqlException (Microsoft.Data.SqlClient) com Number 2601 ou 2627.");
}
