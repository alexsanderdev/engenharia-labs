using System.Text;
using Confluent.Kafka;

namespace F6M03.Kafka.Contratos;

/// <summary>
/// Nomes dos headers usados pelo lab. Metadados (tipo, versão, tentativas, erro) vão em headers;
/// o corpo (value) carrega só o evento em JSON. Pronto: leia, não altere.
/// </summary>
public static class Cabecalhos
{
    /// <summary>Nome do tipo do evento, ex.: "PedidoCriado".</summary>
    public const string TipoEvento = "tipo-evento";

    /// <summary>Versão do contrato do corpo, ex.: "1".</summary>
    public const string VersaoContrato = "versao-contrato";

    /// <summary>Id do evento (duplicado do corpo para facilitar rastreio e deduplicação).</summary>
    public const string EventoId = "evento-id";

    /// <summary>Formato do corpo: "application/json".</summary>
    public const string ContentType = "content-type";

    /// <summary>Quantas tentativas de processamento JÁ falharam (ausente = 0).</summary>
    public const string Tentativas = "x-tentativas";

    /// <summary>Tipo da última exceção (ex.: "System.TimeoutException").</summary>
    public const string ErroTipo = "x-erro-tipo";

    /// <summary>Mensagem da última exceção.</summary>
    public const string ErroMensagem = "x-erro-mensagem";

    /// <summary>Tópico onde a mensagem nasceu (preservado através dos retries).</summary>
    public const string TopicoOriginal = "x-topico-original";

    /// <summary>Partição de onde a mensagem que falhou foi lida.</summary>
    public const string ParticaoOriginal = "x-particao-original";

    /// <summary>Offset de onde a mensagem que falhou foi lida.</summary>
    public const string OffsetOriginal = "x-offset-original";

    /// <summary>Lê o ÚLTIMO valor de um header como texto UTF-8 (ou null se não existir).</summary>
    public static string? Ler(Headers? headers, string nome)
    {
        if (headers is null) return null;
        return headers.TryGetLastBytes(nome, out var bytes) ? Encoding.UTF8.GetString(bytes) : null;
    }

    /// <summary>Adiciona um header de texto UTF-8.</summary>
    public static void Escrever(Headers headers, string nome, string valor) =>
        headers.Add(nome, Encoding.UTF8.GetBytes(valor));
}
