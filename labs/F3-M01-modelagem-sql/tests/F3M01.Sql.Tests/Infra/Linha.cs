using System.Globalization;
using Microsoft.Data.SqlClient;

namespace F3M01.Sql.Tests.Infra;

/// <summary>Uma linha de resultado, acessada pelo NOME da coluna (a ordem das colunas não importa).</summary>
public sealed class Linha(IReadOnlyDictionary<string, object?> valores)
{
    public object? this[string coluna]
    {
        get
        {
            if (!valores.TryGetValue(coluna, out var valor))
                throw new InvalidOperationException(
                    $"A consulta não devolveu a coluna '{coluna}'. Colunas devolvidas: {string.Join(", ", valores.Keys)}.");
            return valor;
        }
    }

    public int Inteiro(string coluna) => Convert.ToInt32(this[coluna] ?? throw Nulo(coluna), CultureInfo.InvariantCulture);
    public decimal Valor(string coluna) => Convert.ToDecimal(this[coluna] ?? throw Nulo(coluna), CultureInfo.InvariantCulture);
    public string Texto(string coluna) => Convert.ToString(this[coluna] ?? throw Nulo(coluna), CultureInfo.InvariantCulture)!.Trim();

    private static InvalidOperationException Nulo(string coluna) => new($"A coluna '{coluna}' veio NULL.");

    /// <summary>
    /// Executa o comando, percorre TODOS os result sets (para que erros no meio do lote virem exceção)
    /// e devolve as linhas do último result set que tem colunas.
    /// </summary>
    public static async Task<List<Linha>> LerTodasAsync(SqlCommand cmd)
    {
        var ultimo = new List<Linha>();
        await using var reader = await cmd.ExecuteReaderAsync();
        do
        {
            if (reader.FieldCount == 0) continue;

            var linhas = new List<Linha>();
            while (await reader.ReadAsync())
            {
                var valores = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                    valores[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                linhas.Add(new Linha(valores));
            }
            ultimo = linhas;
        } while (await reader.NextResultAsync());

        return ultimo;
    }
}
