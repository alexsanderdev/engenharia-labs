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
        ex.InnerException is SqlException { Number: 2601 or 2627 };
}
