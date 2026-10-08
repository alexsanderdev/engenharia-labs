using F6M05.Sagas.Mensageria;

namespace F6M05.Sagas.Saga;

/// <summary>PRONTO. Para onde a saga manda os comandos.</summary>
public interface IPublicadorDeComandos
{
    Task PublicarAsync(ComandoSaga comando, CancellationToken ct = default);
}

/// <summary>
/// PRONTO. Nomes da topologia da saga no RabbitMQ. O prefixo isola ambientes (e testes).
/// </summary>
/// <param name="Prefixo">Ex.: <c>orderflow</c>.</param>
public sealed record NomesDaSaga(string Prefixo)
{
    /// <summary>Exchange direct dos comandos; routing key = tipo do comando (ex.: <c>ReservarEstoque</c>).</summary>
    public string ExchangeDeComandos => $"{Prefixo}.saga.comandos";

    /// <summary>Fila de entrada da saga (eventos dos participantes e <see cref="PedidoCriado"/>).</summary>
    public string FilaDeEventos => $"{Prefixo}.saga.eventos";
}

/// <summary>
/// Publica comandos no exchange de comandos da saga, com confirmação do broker, MessageId
/// determinístico do comando, <c>type</c> = tipo do comando e <c>correlation_id</c> = pedido.
/// </summary>
public sealed class PublicadorDeComandosRabbit(CanalDePublicacao canal, NomesDaSaga nomes) : IPublicadorDeComandos
{
    public Task PublicarAsync(ComandoSaga comando, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(comando);
        // Serializa pelo tipo BASE (ComandoSaga) para o JSON levar o discriminador "$tipo".
        return canal.PublicarJsonAsync<ComandoSaga>(
            nomes.ExchangeDeComandos, comando.Tipo, comando, comando.MessageId, comando.Tipo,
            comando.PedidoId.ToString(), ct);
    }
}
