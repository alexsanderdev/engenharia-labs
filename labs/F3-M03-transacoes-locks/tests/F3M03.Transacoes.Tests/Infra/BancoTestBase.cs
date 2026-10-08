using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace F3M03.Transacoes.Tests.Infra;

/// <summary>
/// PRONTA. Base dos testes de banco: recarrega os dados antes de cada teste e oferece
/// helpers de sessão, de leitura e de OBSERVAÇÃO de bloqueios.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public abstract class BancoTestBase(BancoFixture fixture) : IAsyncLifetime
{
    private readonly List<IAsyncDisposable> _descartaveis = [];

    protected BancoFixture Fixture { get; } = fixture;

    /// <summary>Connection string do banco "Locking" (o padrão dos testes).</summary>
    protected string Cs => Fixture.Locking;

    public virtual async ValueTask InitializeAsync() => await Fixture.ResetarAsync();

    public virtual async ValueTask DisposeAsync()
    {
        foreach (var d in _descartaveis) await d.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Abre uma "aba" A, B... no banco Locking (ou no banco RCSI). Fecha sozinha no fim do teste.</summary>
    protected async Task<Sessao> AbrirSessaoAsync(string nome, bool rcsi = false)
    {
        var sessao = await Sessao.AbrirAsync(rcsi ? Fixture.Rcsi : Fixture.Locking, nome);
        _descartaveis.Add(sessao);
        return sessao;
    }

    /// <summary>Lê o estoque atual (sessão de infraestrutura, fora de qualquer transação de teste).</summary>
    protected async Task<int> EstoqueAsync(int produtoId) =>
        await EscalarInfraAsync<int>($"SELECT Estoque FROM dbo.Produtos WHERE Id = {produtoId};");

    /// <summary>Executa uma consulta escalar numa sessão de infraestrutura no banco Locking.</summary>
    protected async Task<T> EscalarInfraAsync<T>(string sql)
    {
        await using var conexao = new SqlConnection(BancoFixture.ComApp(Fixture.Locking, BancoFixture.AppDaInfra));
        await conexao.OpenAsync();
        await using var cmd = new SqlCommand(sql, conexao);
        return (T)Convert.ChangeType((await cmd.ExecuteScalarAsync())!, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Espera até a sessão <paramref name="spid"/> estar BLOQUEADA (esperando lock de outra).
    /// É polling de uma condição observável em <c>sys.dm_exec_requests</c>, não um "sleep e torcer":
    /// o teste só segue quando o bloqueio realmente existe.
    /// </summary>
    protected Task EsperarBloqueioDaSessaoAsync(int spid) =>
        EsperarCondicaoAsync(
            $"SELECT COUNT(*) FROM sys.dm_exec_requests WHERE session_id = {spid} AND blocking_session_id <> 0;",
            $"a sessão {spid} ficar bloqueada");

    /// <summary>
    /// Espera até existirem pelo menos <paramref name="quantidade"/> sessões do teste
    /// (Application Name = F3M03) bloqueadas. Útil quando quem bloqueia é o código do lab,
    /// que abre as próprias conexões e cujo SPID o teste não conhece.
    /// </summary>
    protected Task EsperarSessoesBloqueadasAsync(int quantidade = 1, CancellationToken ct = default) =>
        EsperarCondicaoAsync(
            $"""
            SELECT CASE WHEN COUNT(*) >= {quantidade} THEN 1 ELSE 0 END
              FROM sys.dm_exec_requests AS r
              JOIN sys.dm_exec_sessions AS s ON s.session_id = r.session_id
             WHERE r.blocking_session_id <> 0
               AND s.program_name = N'{BancoFixture.AppDosTestes}';
            """,
            $"{quantidade} sessão(ões) do teste ficarem bloqueadas", ct);

    /// <summary>
    /// Garante que <paramref name="execucao"/> ficou BLOQUEADA esperando lock ANTES de chegar a
    /// <paramref name="pausa"/>. Se ela chegar ao ponto de parada (ou terminar) sem bloquear,
    /// o teste falha com <paramref name="mensagemSeNaoBloquear"/>.
    /// </summary>
    protected async Task GarantirQueFicouBloqueadaAsync(PontoDeParada pausa, Task execucao, string mensagemSeNaoBloquear)
    {
        using var cts = new CancellationTokenSource();
        var bloqueio = EsperarSessoesBloqueadasAsync(1, cts.Token);
        var primeiro = await Task.WhenAny(bloqueio, pausa.Chegou, execucao);
        await cts.CancelAsync();

        if (primeiro == bloqueio)
        {
            await bloqueio; // propaga TimeoutException, se for o caso
            return;
        }
        if (primeiro == execucao) await execucao; // propaga a exceção do código (ex.: TODO)
        throw new ShouldAssertException(mensagemSeNaoBloquear);
    }

    private async Task EsperarCondicaoAsync(string sqlQueDevolve1, string descricao, CancellationToken ct = default)
    {
        await using var conexao = new SqlConnection(BancoFixture.ComApp(Fixture.Locking, "F3M03-Observador"));
        await conexao.OpenAsync(ct);
        var relogio = Stopwatch.StartNew();
        while (relogio.Elapsed < PontoDeParada.TempoMaximo)
        {
            await using var cmd = new SqlCommand(sqlQueDevolve1, conexao);
            if (Convert.ToInt32(await cmd.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture) >= 1) return;
            await Task.Delay(20, ct);
        }
        throw new TimeoutException($"Esperei {PontoDeParada.TempoMaximo.TotalSeconds} s por {descricao}, e não aconteceu.");
    }

    /// <summary>Aguarda uma tarefa com limite de tempo (para um teste quebrado falhar, não travar).</summary>
    protected static async Task<T> NoMaximoAsync<T>(Task<T> tarefa, string oQue)
    {
        try
        {
            return await tarefa.WaitAsync(PontoDeParada.TempoMaximo);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"{oQue} não terminou em {PontoDeParada.TempoMaximo.TotalSeconds} s (ficou bloqueado?).");
        }
    }

    /// <inheritdoc cref="NoMaximoAsync{T}(Task{T}, string)"/>
    protected static async Task NoMaximoAsync(Task tarefa, string oQue)
    {
        try
        {
            await tarefa.WaitAsync(PontoDeParada.TempoMaximo);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"{oQue} não terminou em {PontoDeParada.TempoMaximo.TotalSeconds} s (ficou bloqueado?).");
        }
    }

    /// <summary>
    /// Ao fim de um teste que pode ter falhado no meio da orquestração: espera as tarefas terminarem
    /// (ou desiste) engolindo exceções, para nada vazar para o próximo teste.
    /// </summary>
    protected static async Task DrenarAsync(params Task?[] tarefas)
    {
        foreach (var t in tarefas)
        {
            if (t is null) continue;
            try { await t.WaitAsync(PontoDeParada.TempoMaximo); }
            catch (Exception) { /* já falhou por outro motivo; o próximo reset derruba a sessão */ }
        }
    }
}
