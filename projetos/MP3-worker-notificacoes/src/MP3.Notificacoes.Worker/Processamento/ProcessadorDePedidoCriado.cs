using System.Diagnostics;
using MP3.Contratos;
using MP3.Notificacoes.Worker.Inbox;
using MP3.Notificacoes.Worker.Notificacoes;

namespace MP3.Notificacoes.Worker.Processamento;

public enum ResultadoDoProcessamento
{
    Notificada,
    Duplicada,
}

/// <summary>
/// O caso de uso, sem nada de Service Bus: "dado um PedidoCriado, notifique o cliente UMA vez".
/// <para>
/// Fluxo: abre transação → registra o evento na inbox (duplicata → para aqui) → envia → commit.
/// Falhou o envio? A transação desfaz o registro e o retry tenta de novo. Ficou só uma janela:
/// enviou e caiu ANTES do commit → o retry reenvia. Efeito externo não entra em transação de banco;
/// por isso o EventoId vai como chave de idempotência para o provedor.
/// </para>
/// <para>
/// Trade-off consciente: a transação fica aberta durante o envio (segura uma conexão e o lock da chave).
/// Para provedores lentos, a alternativa é registrar status (Recebida → Enviando → Enviada) com lease — ver o README.
/// </para>
/// </summary>
public sealed class ProcessadorDePedidoCriado(InboxSql inbox, SeletorDeNotificador seletor)
{
    public const string NomeDoConsumidor = "mp3-notificacoes";

    public async Task<ResultadoDoProcessamento> ProcessarAsync(PedidoCriado evento, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(evento);
        var notificador = seletor.Selecionar(evento.CanalPreferido);
        var notificacao = ModeloDeMensagem.Para(evento, notificador.Canal);

        await using var conexao = await inbox.AbrirAsync(ct);
        await using var transacao = (Microsoft.Data.SqlClient.SqlTransaction)await conexao.BeginTransactionAsync(ct);

        var novo = await inbox.TentarRegistrarAsync(conexao, transacao, NomeDoConsumidor, evento.EventoId, evento.PedidoId, notificador.Canal, ct);
        if (!novo)
        {
            await transacao.RollbackAsync(CancellationToken.None);
            Activity.Current?.SetTag("mp3.resultado", "duplicada");
            return ResultadoDoProcessamento.Duplicada;
        }

        using (var envio = Telemetria.Telemetria.Fonte.StartActivity($"notificar {notificador.Canal}", ActivityKind.Client))
        {
            envio?.SetTag("mp3.canal", notificador.Canal);
            envio?.SetTag("mp3.pedido.id", evento.PedidoId);
            await notificador.EnviarAsync(notificacao, ct);
        }

        // Daqui em diante não cancelamos: a notificação já saiu, perder o commit causaria reenvio.
        await transacao.CommitAsync(CancellationToken.None);
        Activity.Current?.SetTag("mp3.resultado", "notificada");
        return ResultadoDoProcessamento.Notificada;
    }
}
