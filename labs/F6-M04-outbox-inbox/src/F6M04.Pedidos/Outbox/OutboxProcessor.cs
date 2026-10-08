using F6M04.Pedidos.Infra;
using F6M04.Pedidos.Mensageria;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace F6M04.Pedidos.Outbox;

/// <summary>
/// Lê lotes de mensagens pendentes da Outbox, publica no broker e marca como processadas.
/// Pode rodar em VÁRIAS instâncias ao mesmo tempo: cada lote é reservado com
/// <c>UPDLOCK, READPAST, ROWLOCK</c> dentro de uma transação, então duas instâncias nunca pegam a mesma linha.
/// </summary>
/// <remarks>
/// Garantia: AT-LEAST-ONCE. Se o processo cair depois de publicar e antes do commit, a transação é desfeita e a
/// mensagem é publicada de novo na próxima rodada — com o MESMO MessageId. Quem consome deduplica (Inbox).
/// </remarks>
public sealed partial class OutboxProcessor(
    IServiceScopeFactory escopos,
    IPublicadorDeMensagens publicador,
    TimeProvider relogio,
    IOptions<OutboxOptions> opcoes,
    ILogger<OutboxProcessor> log) : BackgroundService
{
    private readonly OutboxOptions _opcoes = opcoes.Value;

    /// <summary>
    /// Laço do serviço: processa um lote já na partida e depois a cada <see cref="OutboxOptions.Intervalo"/>,
    /// usando um <see cref="PeriodicTimer"/> ligado ao <see cref="TimeProvider"/> injetado (testável).
    /// Uma falha num lote é registrada e o laço continua; só o cancelamento encerra.
    /// </summary>
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        throw new NotImplementedException(
            "TODO (Passo 6): using var timer = new PeriodicTimer(_opcoes.Intervalo, relogio); do { try { while " +
            "(await ProcessarLoteAsync(stoppingToken) >= _opcoes.TamanhoDoLote) { } } catch (Exception ex) when " +
            "(!stoppingToken.IsCancellationRequested) { LogFalhaNoLote(ex); } } while (await timer.WaitForNextTickAsync(stoppingToken)); " +
            "— e trate o OperationCanceledException do encerramento.");

    /// <summary>
    /// Uma rodada: reserva até <see cref="OutboxOptions.TamanhoDoLote"/> mensagens pendentes, publica cada uma,
    /// registra o resultado e faz commit. Devolve quantas mensagens foram publicadas com sucesso.
    /// </summary>
    /// <remarks>
    /// Regras da reserva (todas no SQL, dentro da transação):
    /// <list type="bullet">
    /// <item>pendente (<c>ProcessadoEm IS NULL</c>), abaixo de <see cref="OutboxOptions.MaximoDeTentativas"/> e com backoff vencido;</item>
    /// <item>sem mensagem ANTERIOR pendente do mesmo agregado (mesma <c>ChaveDeOrdenacao</c>, <c>VersaoDoAgregado</c> menor);</item>
    /// <item>em ordem de <c>Sequencia</c>, com <c>UPDLOCK, READPAST, ROWLOCK</c>.</item>
    /// </list>
    /// Falha ao publicar uma mensagem: incrementa tentativas, guarda o erro e agenda a próxima tentativa — não
    /// derruba o lote. Falha ao gravar o resultado (banco caiu, processo morreu): a exceção sobe, nada é marcado e
    /// as mensagens já publicadas serão publicadas de novo.
    /// </remarks>
    public Task<int> ProcessarLoteAsync(CancellationToken ct = default)
    {
        // TODO (Passos 3 a 5). Roteiro:
        //  1. escopo = escopos.CreateAsyncScope(); db = PedidosDbContext do escopo;
        //     await using var transacao = await db.Database.BeginTransactionAsync(ct);
        //  2. lote = db.OutboxMessages.FromSql($"""SELECT TOP ({tamanho}) m.* FROM dbo.OutboxMessages AS m
        //     WITH (UPDLOCK, READPAST, ROWLOCK) WHERE ... AND NOT EXISTS (...) ORDER BY m.Sequencia""").ToListAsync(ct)
        //     (as regras do WHERE estão no <remarks> acima; use relogio.GetUtcNow() como "agora");
        //  3. para cada mensagem: Tentativas++; try { await publicador.PublicarAsync(m.ParaMensagemDeSaida(), ct);
        //     ProcessadoEm = agora; UltimoErro = null; ProximaTentativaEm = null }
        //     catch (Exception ex) when (!ct.IsCancellationRequested) { UltimoErro = Truncar(...);
        //     ProximaTentativaEm = agora + _opcoes.CalcularEspera(Tentativas); log }
        //  4. await db.SaveChangesAsync(ct); await transacao.CommitAsync(ct); return publicadas;
        //     NÃO engula exceções do SaveChanges/Commit: elas significam "nada foi marcado".
        _ = (escopos, publicador, relogio, log);
        throw new NotImplementedException("TODO (Passos 3 a 5): reservar o lote com UPDLOCK/READPAST, publicar, registrar tentativas/erro e fazer commit.");
    }

    /// <summary>
    /// Limpeza: apaga as mensagens JÁ publicadas há mais de <see cref="OutboxOptions.RetencaoDasProcessadas"/>.
    /// Pendentes e envenenadas nunca são apagadas aqui. Devolve quantas linhas saíram.
    /// </summary>
    public Task<int> LimparProcessadasAsync(CancellationToken ct = default) =>
        throw new NotImplementedException(
            "TODO (Passo 6): db.OutboxMessages.Where(m => m.ProcessadoEm != null && m.ProcessadoEm < agora - retenção)" +
            ".ExecuteDeleteAsync(ct), num escopo próprio.");

    private static string Truncar(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..maximo];

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha numa rodada da Outbox; tentando de novo no próximo intervalo")]
    private partial void LogFalhaNoLote(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao publicar {MessageId} ({Tipo}), tentativa {Tentativas}")]
    private partial void LogFalhaAoPublicar(Guid messageId, string tipo, int tentativas, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Mensagem {MessageId} ({Tipo}) envenenada depois de {Tentativas} tentativas; não será mais tentada")]
    private partial void LogMensagemEnvenenada(Guid messageId, string tipo, int tentativas, Exception ex);
}
