using F6M05.Sagas.Mensageria;
using RabbitMQ.Client;

namespace F6M05.Sagas.Retry;

/// <summary>Resumo de uma mensagem parada na DLQ (o que um operador quer ver antes de reprocessar).</summary>
public sealed record MensagemNaDlq(
    string MessageId,
    string? Tipo,
    string? Motivo,
    string? Erro,
    string? FilaDeOrigem,
    int Tentativas,
    int Reprocessamentos,
    string? FalhouEm,
    string Corpo);

/// <summary>Resultado de um reprocessamento.</summary>
public sealed record ResultadoReprocessamento(int Movidas, int Mantidas);

/// <summary>
/// Ferramenta de operação da DLQ: lista sem remover e devolve mensagens para a fila de origem
/// depois que a causa foi corrigida (deploy do bug fix, cadastro que faltava etc.).
/// </summary>
/// <remarks>
/// Usa <c>basic.get</c> sem auto-ack: as mensagens lidas ficam "emprestadas" ao canal até o
/// ack (removida da DLQ) ou nack com requeue (volta para a DLQ). Cada mensagem movida é
/// publicada na fila de origem COM confirmação do broker e só então recebe ack na DLQ:
/// em caso de queda no meio, o pior caso é duplicar (o consumidor é idempotente), nunca perder.
/// </remarks>
public sealed class ReprocessadorDeDlq(IConnection conexao, TimeProvider? tempo = null)
{
    private readonly TimeProvider _tempo = tempo ?? TimeProvider.System;

    /// <summary>Lista até <paramref name="maximo"/> mensagens da DLQ de <paramref name="fila"/> SEM removê-las.</summary>
    /// <remarks>
    /// TODO (Passo 4): num canal novo, <c>BasicGetAsync(dlq, autoAck: false)</c> em laço até <c>null</c>
    /// ou <paramref name="maximo"/>; converta com <c>MensagemRecebida.De(...)</c> + <see cref="Resumir"/>.
    /// No fim, devolva TODAS para a DLQ: <c>BasicNackAsync(ultimaTag, multiple: true, requeue: true)</c>.
    /// </remarks>
    public Task<IReadOnlyList<MensagemNaDlq>> ListarAsync(string fila, int maximo = 100, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO: liste a DLQ com basic.get sem ack e devolva tudo com nack+requeue (Passo 4).");

    /// <summary>
    /// Move de volta para a fila de origem as mensagens da DLQ que passam no
    /// <paramref name="filtro"/> (todas, se nulo), até <paramref name="maximo"/>.
    /// Zera <see cref="Cabecalhos.Tentativas"/>, remove motivo/erro/falhou-em e incrementa
    /// <see cref="Cabecalhos.Reprocessamentos"/>. MessageId, tipo, correlação e corpo são preservados.
    /// </summary>
    /// <remarks>
    /// TODO (Passo 4): canal de publicação (<see cref="CanalDePublicacao.CriarAsync"/>) + canal de leitura.
    /// Para cada <c>basic.get</c> da DLQ: fora do filtro → guarde a tag para devolver NO FIM (senão o
    /// próximo get a leria de novo); no filtro → publique na fila de origem (header
    /// <see cref="Cabecalhos.FilaDeOrigem"/>, ou <paramref name="fila"/>) com
    /// <see cref="MensagemRecebida.PropriedadesParaRepublicar"/> ajustando os headers, e SÓ ENTÃO dê ack na DLQ.
    /// Use <c>_tempo</c> para um header informativo <c>x-reprocessada-em</c> (opcional).
    /// </remarks>
    public Task<ResultadoReprocessamento> ReprocessarAsync(
        string fila, Func<MensagemNaDlq, bool>? filtro = null, int maximo = 100, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO: mova as mensagens selecionadas da DLQ para a fila de origem (Passo 4).");

    /// <summary>PRONTO. Resumo legível de uma mensagem da DLQ.</summary>
    private static MensagemNaDlq Resumir(MensagemRecebida m) => new(
        m.MessageId,
        m.Tipo,
        Cabecalhos.LerTexto(m.Cabecalhos, Cabecalhos.Motivo),
        Cabecalhos.LerTexto(m.Cabecalhos, Cabecalhos.Erro),
        Cabecalhos.LerTexto(m.Cabecalhos, Cabecalhos.FilaDeOrigem),
        m.TentativasAtrasadas,
        m.Reprocessamentos,
        Cabecalhos.LerTexto(m.Cabecalhos, Cabecalhos.FalhouEm),
        m.CorpoComoTexto);
}
