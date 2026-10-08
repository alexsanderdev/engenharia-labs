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
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_opcoes.Intervalo, relogio);
        try
        {
            do
            {
                try
                {
                    // Se o lote veio cheio, provavelmente tem mais: não espera o próximo tick.
                    while (await ProcessarLoteAsync(stoppingToken) >= _opcoes.TamanhoDoLote) { }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    LogFalhaNoLote(ex);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento normal do host.
        }
    }

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
    public async Task<int> ProcessarLoteAsync(CancellationToken ct = default)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();

        await using var transacao = await db.Database.BeginTransactionAsync(ct);

        var agora = relogio.GetUtcNow();
        var tamanho = _opcoes.TamanhoDoLote;
        var maximo = _opcoes.MaximoDeTentativas;

        var lote = await db.OutboxMessages
            .FromSql($"""
                SELECT TOP ({tamanho}) m.*
                  FROM dbo.OutboxMessages AS m WITH (UPDLOCK, READPAST, ROWLOCK)
                 WHERE m.ProcessadoEm IS NULL
                   AND m.Tentativas < {maximo}
                   AND (m.ProximaTentativaEm IS NULL OR m.ProximaTentativaEm <= {agora})
                   AND NOT EXISTS (SELECT 1
                                     FROM dbo.OutboxMessages AS anterior
                                    WHERE anterior.ChaveDeOrdenacao = m.ChaveDeOrdenacao
                                      AND anterior.VersaoDoAgregado < m.VersaoDoAgregado
                                      AND anterior.ProcessadoEm IS NULL)
                 ORDER BY m.Sequencia
                """)
            .ToListAsync(ct);

        if (lote.Count == 0) return 0;

        var publicadas = 0;
        foreach (var mensagem in lote)
        {
            mensagem.Tentativas++;
            try
            {
                await publicador.PublicarAsync(mensagem.ParaMensagemDeSaida(), ct);

                mensagem.ProcessadoEm = relogio.GetUtcNow();
                mensagem.UltimoErro = null;
                mensagem.ProximaTentativaEm = null;
                publicadas++;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                mensagem.UltimoErro = Truncar($"{ex.GetType().Name}: {ex.Message}", 2000);
                mensagem.ProximaTentativaEm = relogio.GetUtcNow() + _opcoes.CalcularEspera(mensagem.Tentativas);

                if (mensagem.Tentativas >= _opcoes.MaximoDeTentativas)
                    LogMensagemEnvenenada(mensagem.Id, mensagem.Tipo, mensagem.Tentativas, ex);
                else
                    LogFalhaAoPublicar(mensagem.Id, mensagem.Tipo, mensagem.Tentativas, ex);
            }
        }

        await db.SaveChangesAsync(ct);
        await transacao.CommitAsync(ct);
        return publicadas;
    }

    /// <summary>
    /// Limpeza: apaga as mensagens JÁ publicadas há mais de <see cref="OutboxOptions.RetencaoDasProcessadas"/>.
    /// Pendentes e envenenadas nunca são apagadas aqui. Devolve quantas linhas saíram.
    /// </summary>
    public async Task<int> LimparProcessadasAsync(CancellationToken ct = default)
    {
        await using var escopo = escopos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<PedidosDbContext>();

        var limite = relogio.GetUtcNow() - _opcoes.RetencaoDasProcessadas;
        return await db.OutboxMessages
            .Where(m => m.ProcessadoEm != null && m.ProcessadoEm < limite)
            .ExecuteDeleteAsync(ct);
    }

    private static string Truncar(string texto, int maximo) => texto.Length <= maximo ? texto : texto[..maximo];

    [LoggerMessage(Level = LogLevel.Error, Message = "Falha numa rodada da Outbox; tentando de novo no próximo intervalo")]
    private partial void LogFalhaNoLote(Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Falha ao publicar {MessageId} ({Tipo}), tentativa {Tentativas}")]
    private partial void LogFalhaAoPublicar(Guid messageId, string tipo, int tentativas, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Mensagem {MessageId} ({Tipo}) envenenada depois de {Tentativas} tentativas; não será mais tentada")]
    private partial void LogMensagemEnvenenada(Guid messageId, string tipo, int tentativas, Exception ex);
}
