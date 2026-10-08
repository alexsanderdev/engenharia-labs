using F5M05.Api.Idempotencia;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using static F5M05.Api.Tests.Infra.Http;

namespace F5M05.Api.Tests;

/// <summary>Parte (b), Passo 4 — o armazém de idempotência em memória, isolado, com relógio falso.</summary>
public sealed class ArmazemDeIdempotenciaTests
{
    private readonly FakeTimeProvider _relogio = new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly ArmazemDeIdempotenciaEmMemoria _armazem;
    private static readonly RespostaArmazenada Resposta = new(201, "application/json", "/pedidos/1", [1, 2, 3]);

    public ArmazemDeIdempotenciaTests() =>
        _armazem = new ArmazemDeIdempotenciaEmMemoria(_relogio, Options.Create(new IdempotenciaOptions
        {
            Retencao = TimeSpan.FromHours(24),
            TempoMaximoDeProcessamento = TimeSpan.FromSeconds(30),
        }));

    [Fact]
    public async Task Reservar_ChaveNova_Reserva()
    {
        (await _armazem.TentarReservarAsync("k", "h1", Ct)).Situacao.ShouldBe(SituacaoDaReserva.Reservada);
    }

    [Fact]
    public async Task Reservar_ChaveEmProcessamento_InformaEmAndamento()
    {
        await _armazem.TentarReservarAsync("k", "h1", Ct);

        (await _armazem.TentarReservarAsync("k", "h1", Ct)).Situacao.ShouldBe(SituacaoDaReserva.EmAndamento);
    }

    [Fact]
    public async Task Reservar_ChaveConcluida_DevolveARespostaGuardada()
    {
        await _armazem.TentarReservarAsync("k", "h1", Ct);
        await _armazem.ConcluirAsync("k", Resposta, Ct);

        var resultado = await _armazem.TentarReservarAsync("k", "h1", Ct);

        resultado.Situacao.ShouldBe(SituacaoDaReserva.Concluida);
        resultado.Resposta.ShouldBeSameAs(Resposta);
    }

    [Fact]
    public async Task Reservar_MesmaChaveComOutroHash_InformaCorpoDiferente()
    {
        await _armazem.TentarReservarAsync("k", "h1", Ct);
        await _armazem.ConcluirAsync("k", Resposta, Ct);

        (await _armazem.TentarReservarAsync("k", "h2", Ct)).Situacao.ShouldBe(SituacaoDaReserva.CorpoDiferente);
    }

    [Fact]
    public async Task Reservar_DepoisDaRetencao_ReservaDeNovo()
    {
        await _armazem.TentarReservarAsync("k", "h1", Ct);
        await _armazem.ConcluirAsync("k", Resposta, Ct);

        _relogio.Advance(TimeSpan.FromHours(23));
        (await _armazem.TentarReservarAsync("k", "h1", Ct)).Situacao.ShouldBe(SituacaoDaReserva.Concluida, "23 h: ainda guardada");

        _relogio.Advance(TimeSpan.FromHours(1));
        (await _armazem.TentarReservarAsync("k", "h1", Ct)).Situacao.ShouldBe(SituacaoDaReserva.Reservada, "24 h: expirou");
    }

    [Fact]
    public async Task Reservar_ProcessamentoTravadoAlemDoTempoMaximo_PermiteNovaReserva()
    {
        await _armazem.TentarReservarAsync("k", "h1", Ct); // e o processo "morreu" sem concluir nem liberar

        _relogio.Advance(TimeSpan.FromSeconds(29));
        (await _armazem.TentarReservarAsync("k", "h1", Ct)).Situacao.ShouldBe(SituacaoDaReserva.EmAndamento);

        _relogio.Advance(TimeSpan.FromSeconds(1));
        (await _armazem.TentarReservarAsync("k", "h1", Ct)).Situacao.ShouldBe(SituacaoDaReserva.Reservada,
            "uma reserva órfã não pode travar a chave para sempre");
    }

    [Fact]
    public async Task Liberar_PermiteNovaTentativaComAMesmaChave()
    {
        await _armazem.TentarReservarAsync("k", "h1", Ct);
        await _armazem.LiberarAsync("k", Ct);

        (await _armazem.TentarReservarAsync("k", "h1", Ct)).Situacao.ShouldBe(SituacaoDaReserva.Reservada);
    }

    [Fact]
    public async Task Reservar_MuitasChamadasSimultaneasComAMesmaChave_SoUmaReserva()
    {
        // Largada assíncrona (sem Barrier bloqueante, que esgotaria o thread pool): todas as tarefas
        // ficam esperando o mesmo sinal e, liberadas, correm em paralelo no thread pool.
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tarefas = Enumerable.Range(0, 32).Select(_ => Task.Run(async () =>
        {
            await largada.Task;
            return (await _armazem.TentarReservarAsync("k", "h1", Ct)).Situacao;
        }, Ct)).ToArray();

        largada.SetResult();
        var situacoes = await Task.WhenAll(tarefas);

        situacoes.Count(s => s == SituacaoDaReserva.Reservada).ShouldBe(1);
        situacoes.Count(s => s == SituacaoDaReserva.EmAndamento).ShouldBe(31);
    }
}
