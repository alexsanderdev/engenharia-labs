using System.Text.Json;

namespace F6M01.Mensageria.Contratos;

/// <summary>
/// Envelope de uma mensagem: metadados (identidade, correlação, contrato) + corpo JSON.
/// No RabbitMQ, os metadados viram propriedades AMQP (ver <c>MapeamentoAmqp</c>) e o corpo vai como bytes.
/// </summary>
/// <param name="MessageId">Identidade ÚNICA desta mensagem. É o que um consumidor idempotente usa para
/// descartar duplicatas (Fase 6.04).</param>
/// <param name="CorrelationId">Liga todas as mensagens de um mesmo fluxo de negócio (ex.: tudo que
/// nasceu de um pedido). Se ninguém informar, a mensagem inicia um fluxo novo.</param>
/// <param name="Tipo">Nome estável do contrato (ex.: <c>orderflow.pedidos.pedido-criado</c>).</param>
/// <param name="Versao">Versão do contrato.</param>
/// <param name="CriadoEm">Quando a mensagem foi criada (relógio de quem publicou).</param>
/// <param name="Corpo">Payload em JSON UTF-8.</param>
public sealed record Envelope(
    Guid MessageId,
    string CorrelationId,
    string Tipo,
    int Versao,
    DateTimeOffset CriadoEm,
    byte[] Corpo)
{
    /// <summary>Opções de JSON do fio: camelCase, tolerante a campos desconhecidos. (PRONTO)</summary>
    public static readonly JsonSerializerOptions OpcoesJson = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Cria o envelope de <paramref name="mensagem"/>:
    /// <list type="bullet">
    /// <item><see cref="MessageId"/>: <c>Guid.CreateVersion7(relogio.GetUtcNow())</c> (único e ordenável no tempo);</item>
    /// <item><see cref="CorrelationId"/>: o informado ou, se nulo/vazio, o próprio MessageId em texto ("D");</item>
    /// <item><see cref="Tipo"/> e <see cref="Versao"/>: do <see cref="CatalogoDeContratos"/>;</item>
    /// <item><see cref="CriadoEm"/>: <c>relogio.GetUtcNow()</c>;</item>
    /// <item><see cref="Corpo"/>: <c>JsonSerializer.SerializeToUtf8Bytes(mensagem, OpcoesJson)</c>.</item>
    /// </list>
    /// </summary>
    public static Envelope Criar<T>(T mensagem, string? correlationId, TimeProvider relogio) where T : IMensagem
    {
        ArgumentNullException.ThrowIfNull(mensagem);
        ArgumentNullException.ThrowIfNull(relogio);

        throw new NotImplementedException(
            "TODO (Passo 2): MessageId = Guid.CreateVersion7(agora); CorrelationId informado ou o próprio MessageId; " +
            "Tipo/Versao do CatalogoDeContratos; CriadoEm = relogio.GetUtcNow(); Corpo = JsonSerializer.SerializeToUtf8Bytes(mensagem, OpcoesJson)");
    }

    /// <summary>
    /// Lê o corpo como <typeparamref name="T"/>. Antes de desserializar, confere o contrato:
    /// se <see cref="Tipo"/> ou <see cref="Versao"/> não forem os do catálogo para <typeparamref name="T"/>,
    /// lança <see cref="ContratoIncompativelException"/> (nunca "adivinhe" uma versão que você não conhece).
    /// Campos a mais no JSON são ignorados (tolerant reader: mudança aditiva não quebra consumidor).
    /// JSON inválido ou nulo também vira <see cref="ContratoIncompativelException"/>.
    /// </summary>
    public T LerCorpo<T>() where T : IMensagem
    {
        throw new NotImplementedException(
            "TODO (Passo 2): confira Tipo e Versao contra o catálogo (senão ContratoIncompativelException) e " +
            "desserialize com JsonSerializer.Deserialize<T>(Corpo, OpcoesJson); JsonException/nulo → ContratoIncompativelException");
    }
}

/// <summary>A mensagem recebida não corresponde a um contrato que este consumidor entende. (PRONTO)</summary>
public sealed class ContratoIncompativelException : Exception
{
    public ContratoIncompativelException()
    {
    }

    public ContratoIncompativelException(string message) : base(message)
    {
    }

    public ContratoIncompativelException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
