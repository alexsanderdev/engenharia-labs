using System.Text.Json;
using Confluent.Kafka;

namespace F6M03.Kafka.Contratos;

/// <summary>
/// Converte eventos de pedido em mensagens Kafka (chave + JSON + headers) e de volta.
/// </summary>
public static class SerializadorDeEventos
{
    /// <summary>Única versão de contrato que este serviço sabe ler e escrever.</summary>
    public const int VersaoAtual = 1;

    /// <summary>JSON "web": camelCase, case-insensitive na leitura.</summary>
    public static readonly JsonSerializerOptions Opcoes = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Passo 1: monta a mensagem de um evento.
    /// <list type="bullet">
    /// <item><c>Key</c> = <c>PedidoId</c> em texto (mesma chave → mesma partição → ordem garantida).</item>
    /// <item><c>Value</c> = JSON (UTF-8) do evento com <see cref="Opcoes"/>, usando o tipo CONCRETO do evento.</item>
    /// <item>Headers: <see cref="Cabecalhos.TipoEvento"/> (nome do tipo), <see cref="Cabecalhos.VersaoContrato"/>
    /// (<see cref="VersaoAtual"/>), <see cref="Cabecalhos.EventoId"/> e <see cref="Cabecalhos.ContentType"/> ("application/json").</item>
    /// </list>
    /// </summary>
    public static Message<string, byte[]> CriarMensagem(IEventoDePedido evento)
    {
        ArgumentNullException.ThrowIfNull(evento);

        var headers = new Headers();
        Cabecalhos.Escrever(headers, Cabecalhos.TipoEvento, evento.GetType().Name);
        Cabecalhos.Escrever(headers, Cabecalhos.VersaoContrato, VersaoAtual.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Cabecalhos.Escrever(headers, Cabecalhos.EventoId, evento.EventoId.ToString());
        Cabecalhos.Escrever(headers, Cabecalhos.ContentType, "application/json");

        return new Message<string, byte[]>
        {
            Key = evento.PedidoId.ToString(),
            Value = JsonSerializer.SerializeToUtf8Bytes(evento, evento.GetType(), Opcoes),
            Headers = headers,
        };
    }

    /// <summary>
    /// Passo 1: lê o evento de uma mensagem. Usa o header <see cref="Cabecalhos.TipoEvento"/> para escolher o tipo
    /// (<see cref="PedidoCriado"/> ou <see cref="PedidoConfirmado"/>) e exige <see cref="Cabecalhos.VersaoContrato"/> == "1".
    /// Tipo desconhecido, versão diferente, header ausente ou JSON inválido → <see cref="ContratoNaoSuportadoException"/>.
    /// </summary>
    public static IEventoDePedido Ler(Message<string, byte[]> mensagem)
    {
        ArgumentNullException.ThrowIfNull(mensagem);

        var versao = Cabecalhos.Ler(mensagem.Headers, Cabecalhos.VersaoContrato);
        if (versao != VersaoAtual.ToString(System.Globalization.CultureInfo.InvariantCulture))
            throw new ContratoNaoSuportadoException($"Versão de contrato '{versao ?? "(ausente)"}' não suportada (esperado {VersaoAtual}).");

        var tipo = Cabecalhos.Ler(mensagem.Headers, Cabecalhos.TipoEvento) switch
        {
            nameof(PedidoCriado) => typeof(PedidoCriado),
            nameof(PedidoConfirmado) => typeof(PedidoConfirmado),
            var outro => throw new ContratoNaoSuportadoException($"Tipo de evento '{outro ?? "(ausente)"}' desconhecido."),
        };

        try
        {
            return (IEventoDePedido)(JsonSerializer.Deserialize(mensagem.Value, tipo, Opcoes)
                ?? throw new ContratoNaoSuportadoException("Corpo vazio."));
        }
        catch (JsonException ex)
        {
            throw new ContratoNaoSuportadoException("JSON inválido para o contrato.", ex);
        }
    }
}
