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
    public async Task<IReadOnlyList<MensagemNaDlq>> ListarAsync(string fila, int maximo = 100, CancellationToken ct = default)
    {
        await using var canal = await conexao.CreateChannelAsync(cancellationToken: ct);
        var dlq = PoliticaDeRetry.NomeDaDlq(fila);
        var lidas = new List<MensagemNaDlq>();
        ulong ultimaTag = 0;

        while (lidas.Count < maximo)
        {
            var resultado = await canal.BasicGetAsync(dlq, autoAck: false, ct);
            if (resultado is null) break;
            ultimaTag = resultado.DeliveryTag;
            lidas.Add(Resumir(MensagemRecebida.De(dlq, resultado.BasicProperties, resultado.Body, resultado.Redelivered)));
        }

        // Devolve tudo para a DLQ (requeue), na mesma ordem.
        if (ultimaTag > 0)
            await canal.BasicNackAsync(ultimaTag, multiple: true, requeue: true, ct);

        return lidas;
    }

    /// <summary>
    /// Move de volta para a fila de origem as mensagens da DLQ que passam no
    /// <paramref name="filtro"/> (todas, se nulo), até <paramref name="maximo"/>.
    /// Zera <see cref="Cabecalhos.Tentativas"/>, remove motivo/erro/falhou-em e incrementa
    /// <see cref="Cabecalhos.Reprocessamentos"/>. MessageId, tipo, correlação e corpo são preservados.
    /// </summary>
    public async Task<ResultadoReprocessamento> ReprocessarAsync(
        string fila, Func<MensagemNaDlq, bool>? filtro = null, int maximo = 100, CancellationToken ct = default)
    {
        await using var publicacao = await CanalDePublicacao.CriarAsync(conexao, ct);
        await using var canal = await conexao.CreateChannelAsync(cancellationToken: ct);
        var dlq = PoliticaDeRetry.NomeDaDlq(fila);
        var movidas = 0;
        var mantidas = new List<ulong>();

        for (var lidas = 0; lidas < maximo; lidas++)
        {
            var resultado = await canal.BasicGetAsync(dlq, autoAck: false, ct);
            if (resultado is null) break;

            var mensagem = MensagemRecebida.De(dlq, resultado.BasicProperties, resultado.Body, resultado.Redelivered);
            var resumo = Resumir(mensagem);
            if (filtro is not null && !filtro(resumo))
            {
                mantidas.Add(resultado.DeliveryTag);
                continue;
            }

            var destino = resumo.FilaDeOrigem ?? fila;
            var propriedades = mensagem.PropriedadesParaRepublicar(h =>
            {
                h.Remove(Cabecalhos.Tentativas);
                h.Remove(Cabecalhos.Motivo);
                h.Remove(Cabecalhos.Erro);
                h.Remove(Cabecalhos.FalhouEm);
                h.Remove(Cabecalhos.FilaDeOrigem);
                h[Cabecalhos.Reprocessamentos] = resumo.Reprocessamentos + 1;
                h["x-reprocessada-em"] = _tempo.GetUtcNow().ToString("O");
            });

            await publicacao.PublicarAsync("", destino, propriedades, mensagem.Corpo, ct);
            await canal.BasicAckAsync(resultado.DeliveryTag, multiple: false, ct);
            movidas++;
        }

        // As não selecionadas voltam para a DLQ. Só no fim: se voltassem antes, o próximo
        // basic.get as leria de novo (loop).
        foreach (var tag in mantidas)
            await canal.BasicNackAsync(tag, multiple: false, requeue: true, ct);

        return new ResultadoReprocessamento(movidas, mantidas.Count);
    }

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
