using System.Text.Json;
using F6M04.Notificacoes.Contratos;
using F6M04.Notificacoes.Dominio;
using F6M04.Notificacoes.Infra;
using Microsoft.Data.SqlClient; // SqlException 2627/2601
using Microsoft.EntityFrameworkCore;

namespace F6M04.Notificacoes.Inbox;

/// <summary>
/// Consumidor IDEMPOTENTE dos eventos de Pedidos. O broker entrega "pelo menos uma vez" (a Outbox republica, o
/// broker reentrega depois de queda de conexão...). A Inbox transforma isso em efeito "exatamente uma vez":
/// o <c>MessageId</c> é gravado na MESMA transação do efeito colateral; se já estiver lá, a mensagem é
/// reconhecida sem repetir o efeito.
/// </summary>
public sealed class ConsumidorDeNotificacoes(NotificacoesDbContext db, TimeProvider relogio)
{
    /// <summary>Nome deste consumidor na Inbox (outros consumidores da mesma mensagem usam outro nome).</summary>
    public const string Nome = "notificacoes";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,          // "clienteEmail": null -> JsonException (não vira NULL no banco)
        RespectRequiredConstructorParameters = true, // campo obrigatório ausente -> JsonException
    };

    /// <summary>
    /// Processa uma entrega. Devolve <see cref="ResultadoDoProcessamento.Duplicada"/> se o MessageId já foi
    /// processado (inclusive quando duas entregas iguais chegam AO MESMO TEMPO). Lança
    /// <see cref="MensagemInvalidaException"/> para mensagens que nunca vão funcionar (vão para a DLQ). Qualquer
    /// outra exceção significa "nada foi gravado": a mensagem pode ser reprocessada com segurança.
    /// </summary>
    public Task<ResultadoDoProcessamento> ProcessarAsync(MensagemRecebida mensagem, CancellationToken ct = default)
    {
        // TODO (Passo 7). Roteiro:
        //  1. MessageId vazio → MensagemInvalidaException (não dá para deduplicar).
        //  2. Já existe InboxMessage (MessageId, Nome)? → Duplicada (caminho rápido).
        //  3. notificacao = CriarNotificacao(mensagem, agora) (pronto, lá embaixo; null = tipo ignorado);
        //     adicione a notificação (se houver) E a InboxMessage, e chame SaveChangesAsync UMA vez (mesma transação).
        //  4. DbUpdateException com SqlException 2627/2601 = outra entrega igual gravou primeiro → ChangeTracker.Clear()
        //     e Duplicada. Qualquer outra exceção: deixe subir (nada foi gravado; a reentrega reprocessa).
        //  5. Processada (ou Ignorada se não havia notificação).
        _ = (db, relogio, mensagem);
        throw new NotImplementedException("TODO (Passo 7): Inbox + efeito no mesmo SaveChanges; duplicata (inclusive concorrente) = ack sem efeito.");
    }

    private static Notificacao? CriarNotificacao(MensagemRecebida mensagem, DateTimeOffset agora)
    {
        switch (mensagem.Tipo)
        {
            case PedidoCriadoRecebido.Tipo:
                var criado = Ler<PedidoCriadoRecebido>(mensagem);
                return new Notificacao(criado.PedidoId, mensagem.Tipo, criado.ClienteEmail,
                    $"Recebemos o seu pedido {criado.Numero} no valor de {criado.Total:N2}.", agora);

            case PedidoConfirmadoRecebido.Tipo:
                var confirmado = Ler<PedidoConfirmadoRecebido>(mensagem);
                return new Notificacao(confirmado.PedidoId, mensagem.Tipo, confirmado.ClienteEmail,
                    $"O seu pedido {confirmado.Numero} foi confirmado.", agora);

            default:
                return null; // tipo que não nos interessa: registra na Inbox e segue
        }
    }

    private static T Ler<T>(MensagemRecebida mensagem)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(mensagem.Corpo, Json)
                ?? throw new MensagemInvalidaException($"Corpo vazio na mensagem {mensagem.MessageId}.");
        }
        catch (JsonException ex)
        {
            throw new MensagemInvalidaException($"JSON inválido na mensagem {mensagem.MessageId} ({mensagem.Tipo}).", ex);
        }
    }
}
