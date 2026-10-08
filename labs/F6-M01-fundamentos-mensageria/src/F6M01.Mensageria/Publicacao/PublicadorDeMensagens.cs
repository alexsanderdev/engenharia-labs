using F6M01.Mensageria.Contratos;
using RabbitMQ.Client;

namespace F6M01.Mensageria.Publicacao;

/// <summary>
/// Publica comandos e eventos do OrderFlow. A API separa os dois de propósito:
/// <see cref="EnviarAsync{TComando}"/> só aceita <see cref="IComando"/> e <see cref="PublicarAsync{TEvento}"/>
/// só aceita <see cref="IEvento"/>.
/// </summary>
/// <remarks>
/// Usa um canal com <b>publisher confirms</b>: o <c>await</c> de cada publicação só termina quando o broker
/// confirma que assumiu a mensagem (ou lança se ele recusar). Sem isso, "publiquei" significa só
/// "escrevi no socket".
/// </remarks>
public sealed class PublicadorDeMensagens : IAsyncDisposable
{
    private readonly IChannel _canal;
    private readonly TimeProvider _relogio;

    private PublicadorDeMensagens(IChannel canal, TimeProvider relogio)
    {
        _canal = canal;
        _relogio = relogio;
    }

    /// <summary>
    /// Cria o publicador com um canal próprio, com publisher confirms LIGADOS e com rastreamento:
    /// <c>new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true)</c>.
    /// </summary>
    public static async Task<PublicadorDeMensagens> CriarAsync(
        IConnection conexao, TimeProvider? relogio = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(conexao);

        await Task.CompletedTask;
        throw new NotImplementedException(
            "TODO (Passo 5): crie o canal com publisher confirms e devolva new PublicadorDeMensagens(canal, relogio ?? TimeProvider.System)");
    }

    /// <summary>
    /// Envia um COMANDO para o seu único destinatário, com <c>mandatory: true</c>: se nenhuma fila
    /// receber a mensagem, o broker a devolve e a chamada LANÇA (um comando sem destinatário é bug).
    /// </summary>
    public Task<Envelope> EnviarAsync<TComando>(TComando comando, string? correlationId = null, CancellationToken ct = default)
        where TComando : IComando =>
        PublicarInternoAsync(comando, correlationId, obrigatorio: true, ct);

    /// <summary>
    /// Publica um EVENTO na exchange de eventos, com <c>mandatory: false</c>: um evento sem
    /// assinantes é normal (ninguém se interessou ainda) e não é erro.
    /// </summary>
    public Task<Envelope> PublicarAsync<TEvento>(TEvento evento, string? correlationId = null, CancellationToken ct = default)
        where TEvento : IEvento =>
        PublicarInternoAsync(evento, correlationId, obrigatorio: false, ct);

    /// <summary>
    /// Cria o <see cref="Envelope"/>, busca o contrato no <see cref="CatalogoDeContratos"/> e chama
    /// <c>BasicPublishAsync(contrato.Exchange, contrato.RoutingKey, obrigatorio, MapeamentoAmqp.ParaPropriedades(envelope), envelope.Corpo, ct)</c>.
    /// Devolve o envelope publicado.
    /// </summary>
    private async Task<Envelope> PublicarInternoAsync<T>(T mensagem, string? correlationId, bool obrigatorio, CancellationToken ct)
        where T : IMensagem
    {
        await Task.CompletedTask;
        throw new NotImplementedException(
            "TODO (Passo 5): Envelope.Criar(mensagem, correlationId, _relogio), contrato do catálogo e " +
            "_canal.BasicPublishAsync(exchange, routingKey, mandatory: obrigatorio, propriedades, corpo, ct)");
    }

    /// <summary>Fecha o canal. (PRONTO)</summary>
    public async ValueTask DisposeAsync()
    {
        if (_canal.IsOpen)
            await _canal.CloseAsync();
        await _canal.DisposeAsync();
    }
}
