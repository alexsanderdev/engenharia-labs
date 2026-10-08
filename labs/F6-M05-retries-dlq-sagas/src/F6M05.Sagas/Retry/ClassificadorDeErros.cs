using System.Reflection;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace F6M05.Sagas.Retry;

/// <summary>Como o consumidor deve tratar uma exceção.</summary>
public enum TipoDeErro
{
    /// <summary>Vale repetir (com limite): retry imediato, depois atrasado, depois DLQ.</summary>
    Transitorio,

    /// <summary>Não vale repetir: DLQ direto.</summary>
    Permanente,
}

/// <summary>
/// Decide se uma exceção é transitória ou permanente. É a decisão mais importante do retry:
/// repetir erro permanente atrasa a fila e esconde bug; não repetir erro transitório perde
/// mensagem boa.
/// </summary>
public class ClassificadorDeErros
{
    /// <summary>Erros de SQL Server que costumam passar sozinhos (deadlock, timeout, failover do Azure SQL).</summary>
    private static readonly HashSet<int> ErrosSqlTransitorios = [-2, 1205, 4060, 40197, 40501, 40613, 49918, 49919, 49920];

    /// <summary>
    /// Regras, em ordem:
    /// <list type="number">
    /// <item>Desembrulha <see cref="AggregateException"/> com UMA interna e <see cref="TargetInvocationException"/>.</item>
    /// <item><see cref="ErroPermanenteException"/> → Permanente; <see cref="ErroTransitorioException"/> (e derivadas) → Transitório.</item>
    /// <item>Mensagem inválida (<see cref="JsonException"/>, <see cref="FormatException"/>,
    /// <see cref="ArgumentException"/> e derivadas, <see cref="InvalidCastException"/>,
    /// <see cref="NotSupportedException"/>) → Permanente: repetir não muda o conteúdo.</item>
    /// <item>Infraestrutura (<see cref="TimeoutException"/>, <see cref="HttpRequestException"/>,
    /// <see cref="IOException"/>) → Transitório. <see cref="SqlException"/>: transitória só para os números conhecidos.</item>
    /// <item>Qualquer outra → Transitório (na dúvida, repete; o limite de tentativas e a DLQ protegem).</item>
    /// </list>
    /// </summary>
    public virtual TipoDeErro Classificar(Exception erro)
    {
        ArgumentNullException.ThrowIfNull(erro);

        erro = Desembrulhar(erro);

        return erro switch
        {
            ErroPermanenteException => TipoDeErro.Permanente,
            ErroTransitorioException => TipoDeErro.Transitorio,
            JsonException or FormatException or ArgumentException or InvalidCastException or NotSupportedException
                => TipoDeErro.Permanente,
            TimeoutException or HttpRequestException or IOException => TipoDeErro.Transitorio,
            SqlException sql => ErrosSqlTransitorios.Contains(sql.Number) ? TipoDeErro.Transitorio : TipoDeErro.Permanente,
            _ => TipoDeErro.Transitorio,
        };
    }

    private static Exception Desembrulhar(Exception erro)
    {
        while (true)
        {
            switch (erro)
            {
                case AggregateException { InnerExceptions.Count: 1 } agregada:
                    erro = agregada.InnerExceptions[0];
                    continue;
                case TargetInvocationException { InnerException: { } interna }:
                    erro = interna;
                    continue;
                default:
                    return erro;
            }
        }
    }
}
