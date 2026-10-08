using System.Text.Json;
using F6M04.Pedidos.Dominio;
using F6M04.Pedidos.Mensageria;

namespace F6M04.Pedidos.Outbox;

/// <summary>
/// PRONTO. Uma linha da tabela <c>OutboxMessages</c>: o evento serializado, gravado na MESMA transação
/// do agregado, esperando o <see cref="OutboxProcessor"/> publicá-lo no broker.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Serialização do payload: a mesma convenção (camelCase) que os consumidores esperam.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private OutboxMessage() { } // EF Core

    /// <summary>
    /// Identidade da mensagem: vira o <c>MessageId</c> no broker e é a chave de deduplicação do consumidor.
    /// Gerada UMA vez, na gravação: toda republicação usa o mesmo valor.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Ordem GLOBAL de gravação (IDENTITY, índice clusterizado): o processor varre a tabela nesta ordem (justiça
    /// entre agregados). NÃO serve para ordenar eventos de um mesmo agregado gravados no mesmo <c>SaveChanges</c>:
    /// o EF Core não garante a ordem dos INSERTs dentro de um lote. Para isso existe <see cref="VersaoDoAgregado"/>.
    /// </summary>
    public long Sequencia { get; private set; }

    /// <summary>Nome do evento (<see cref="IEventoDeIntegracao.Tipo"/>).</summary>
    public string Tipo { get; private set; } = "";

    /// <summary>Mensagens com a mesma chave (o mesmo pedido) saem na ordem de <see cref="VersaoDoAgregado"/>.</summary>
    public string ChaveDeOrdenacao { get; private set; } = "";

    /// <summary>
    /// Posição do evento na história do agregado (1, 2, 3...). A ordem por agregado usa ESTA coluna: o evento N só
    /// sai depois que o N-1 saiu. Índice único (<see cref="ChaveDeOrdenacao"/>, <see cref="VersaoDoAgregado"/>).
    /// </summary>
    public int VersaoDoAgregado { get; private set; }

    /// <summary>Evento serializado em JSON.</summary>
    public string Payload { get; private set; } = "";

    public DateTimeOffset OcorridoEm { get; private set; }

    /// <summary>Quando foi publicada com confirmação do broker. <c>null</c> = pendente.</summary>
    public DateTimeOffset? ProcessadoEm { get; set; }

    /// <summary>Quantas vezes o processor TENTOU publicar (sucesso ou falha) e registrou o resultado.</summary>
    public int Tentativas { get; set; }

    /// <summary>Mensagem da última falha (para o painel de operação). Limpa quando publica.</summary>
    public string? UltimoErro { get; set; }

    /// <summary>Backoff: antes deste instante a mensagem não é tentada de novo. <c>null</c> = já pode.</summary>
    public DateTimeOffset? ProximaTentativaEm { get; set; }

    /// <summary>
    /// Uma linha por evento PENDENTE do agregado, na ordem em que aconteceram, com a versão de cada um
    /// (o último evento tem a versão atual do agregado).
    /// </summary>
    public static IReadOnlyList<OutboxMessage> DoAgregado(ITemEventos agregado)
    {
        ArgumentNullException.ThrowIfNull(agregado);
        var primeiraVersao = agregado.Versao - agregado.Eventos.Count + 1;
        return [.. agregado.Eventos.Select((evento, i) => De(evento, primeiraVersao + i))];
    }

    /// <summary>Cria a linha a partir de um evento de integração (serializa o tipo concreto).</summary>
    public static OutboxMessage De(IEventoDeIntegracao evento, int versaoDoAgregado) => new()
    {
        VersaoDoAgregado = versaoDoAgregado,
        Id = Guid.CreateVersion7(evento.OcorridoEm),
        Tipo = evento.Tipo,
        ChaveDeOrdenacao = evento.ChaveDeOrdenacao,
        Payload = JsonSerializer.Serialize(evento, evento.GetType(), Json),
        OcorridoEm = evento.OcorridoEm,
    };

    /// <summary>O que vai para o publicador (o broker não precisa saber de colunas de controle).</summary>
    public MensagemDeSaida ParaMensagemDeSaida() => new(Id, Tipo, ChaveDeOrdenacao, Payload, OcorridoEm);
}
