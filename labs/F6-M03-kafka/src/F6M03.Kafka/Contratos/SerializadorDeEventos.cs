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
    public static Message<string, byte[]> CriarMensagem(IEventoDePedido evento) =>
        throw new NotImplementedException(
            "TODO (Passo 1): crie new Headers() e use Cabecalhos.Escrever para tipo-evento (evento.GetType().Name), " +
            "versao-contrato (\"1\"), evento-id e content-type; devolva new Message<string, byte[]> com Key = PedidoId.ToString() " +
            "e Value = JsonSerializer.SerializeToUtf8Bytes(evento, evento.GetType(), Opcoes).");

    /// <summary>
    /// Passo 1: lê o evento de uma mensagem. Usa o header <see cref="Cabecalhos.TipoEvento"/> para escolher o tipo
    /// (<see cref="PedidoCriado"/> ou <see cref="PedidoConfirmado"/>) e exige <see cref="Cabecalhos.VersaoContrato"/> == "1".
    /// Tipo desconhecido, versão diferente, header ausente ou JSON inválido → <see cref="ContratoNaoSuportadoException"/>.
    /// </summary>
    public static IEventoDePedido Ler(Message<string, byte[]> mensagem) =>
        throw new NotImplementedException(
            "TODO (Passo 1): valide o header versao-contrato (== \"1\"), escolha o tipo pelo header tipo-evento " +
            "(PedidoCriado/PedidoConfirmado) e desserialize com JsonSerializer.Deserialize(mensagem.Value, tipo, Opcoes). " +
            "Qualquer problema (versão, tipo, JsonException) vira ContratoNaoSuportadoException.");
}
