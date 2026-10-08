using System.Text.Json;
using F6M04.Notificacoes.Contratos;
using F6M04.Notificacoes.Dominio;
using F6M04.Notificacoes.Infra;
using Microsoft.Data.SqlClient;
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
    public async Task<ResultadoDoProcessamento> ProcessarAsync(MensagemRecebida mensagem, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(mensagem);
        if (string.IsNullOrWhiteSpace(mensagem.MessageId))
            throw new MensagemInvalidaException("Mensagem sem MessageId não pode ser deduplicada.");

        // 1) Caminho rápido: já processada? (a garantia de verdade é a PK, no passo 3)
        var jaProcessada = await db.InboxMessages
            .AnyAsync(i => i.MessageId == mensagem.MessageId && i.Consumidor == Nome, ct);
        if (jaProcessada) return ResultadoDoProcessamento.Duplicada;

        // 2) Efeito colateral + registro na Inbox, no mesmo SaveChanges (= mesma transação).
        var agora = relogio.GetUtcNow();
        var notificacao = CriarNotificacao(mensagem, agora);
        if (notificacao is not null) db.Notificacoes.Add(notificacao);
        db.InboxMessages.Add(new InboxMessage(mensagem.MessageId, Nome, mensagem.Tipo, agora));

        // 3) Corrida: duas entregas iguais passaram pelo passo 1 ao mesmo tempo. A PK da Inbox deixa só uma
        //    gravar; a outra recebe violação de chave (2627/2601), a transação dela é desfeita (o efeito
        //    também) e ela é tratada como duplicada.
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2627 or 2601 })
        {
            db.ChangeTracker.Clear();
            return ResultadoDoProcessamento.Duplicada;
        }

        return notificacao is null ? ResultadoDoProcessamento.Ignorada : ResultadoDoProcessamento.Processada;
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
