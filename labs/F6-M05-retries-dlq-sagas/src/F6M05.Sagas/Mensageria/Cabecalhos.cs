using System.Globalization;
using System.Text;

namespace F6M05.Sagas.Mensageria;

/// <summary>
/// PRONTO. Nomes dos headers AMQP usados pelo retry, pela DLQ e pelo reprocessamento,
/// e helpers para ler valores de header.
/// </summary>
/// <remarks>
/// Cuidado clássico do RabbitMQ.Client: um header publicado como <c>string</c> volta como
/// <c>byte[]</c> (o AMQP não tem tipo "string" em tabelas, só "long string" em bytes). Um
/// <c>int</c> volta como <c>int</c>, mas um valor vindo de outro cliente pode chegar como
/// <c>long</c>. Por isso os helpers aceitam os três formatos.
/// </remarks>
public static class Cabecalhos
{
    /// <summary>Quantas retentativas ATRASADAS (via fila de espera) a mensagem já fez.</summary>
    public const string Tentativas = "x-tentativas";

    /// <summary>Motivo da ida para a DLQ (<see cref="MotivoDeadLetter"/>).</summary>
    public const string Motivo = "x-motivo";

    /// <summary>Tipo e mensagem da última exceção (truncado).</summary>
    public const string Erro = "x-erro";

    /// <summary>Fila de onde a mensagem saiu (o reprocessador devolve para ela).</summary>
    public const string FilaDeOrigem = "x-fila-origem";

    /// <summary>Instante (ISO 8601, UTC) em que a mensagem foi para a DLQ.</summary>
    public const string FalhouEm = "x-falhou-em";

    /// <summary>Quantas vezes a mensagem já foi reprocessada a partir da DLQ.</summary>
    public const string Reprocessamentos = "x-reprocessamentos";

    /// <summary>Lê um header textual (aceita <c>byte[]</c> e <c>string</c>).</summary>
    public static string? LerTexto(IReadOnlyDictionary<string, object?>? cabecalhos, string nome) =>
        cabecalhos is not null && cabecalhos.TryGetValue(nome, out var valor)
            ? valor switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string texto => texto,
                null => null,
                _ => Convert.ToString(valor, CultureInfo.InvariantCulture),
            }
            : null;

    /// <summary>Lê um header numérico (aceita <c>int</c>, <c>long</c>, <c>byte[]</c> e <c>string</c>); 0 se ausente.</summary>
    public static int LerInteiro(IReadOnlyDictionary<string, object?>? cabecalhos, string nome) =>
        cabecalhos is not null && cabecalhos.TryGetValue(nome, out var valor)
            ? valor switch
            {
                int i => i,
                long l => (int)l,
                short s => s,
                byte b => b,
                byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var n) => n,
                string texto when int.TryParse(texto, out var n) => n,
                _ => 0,
            }
            : 0;

    /// <summary>Converte o dicionário de headers do RabbitMQ.Client para leitura.</summary>
    public static IReadOnlyDictionary<string, object?> Copiar(IDictionary<string, object?>? origem) =>
        origem is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(origem);
}

/// <summary>Por que a mensagem foi parar na DLQ.</summary>
public enum MotivoDeadLetter
{
    /// <summary>Erro que não melhora repetindo (mensagem inválida, regra violada, bug determinístico).</summary>
    ErroPermanente,

    /// <summary>Erro transitório que persistiu além de todas as retentativas imediatas e atrasadas.</summary>
    RetentativasEsgotadas,
}
