using System.Diagnostics;
using F6M05.Sagas.Mensageria;
using F6M05.Sagas.Retry;
using F6M05.Tests.Infra;

namespace F6M05.Tests.Parte1_Retry;

/// <summary>Passo 3 — retry imediato, retry atrasado, erro permanente e mensagem venenosa.</summary>
public sealed class ConsumidorComRetryTests(InfraFixture infra)
{
    /// <summary>2 imediatas × 2 atrasadas curtas: pior caso = (1+2) × (1+2) = 9 execuções.</summary>
    internal static readonly PoliticaDeRetry Politica = new()
    {
        RetentativasImediatas = 2,
        Atrasos = [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300)],
    };

    internal static async Task<ConsumidorComRetry> IniciarAsync(InfraFixture infra, string fila, PoliticaDeRetry politica, ManipuladorRoteirizado manipulador)
    {
        await using (var canal = await infra.Rabbit.CreateChannelAsync())
            await TopologiaDeRetry.DeclararAsync(canal, fila, politica);
        var consumidor = new ConsumidorComRetry(infra.Rabbit, fila, politica, manipulador.ProcessarAsync);
        await consumidor.IniciarAsync();
        return consumidor;
    }

    [Fact]
    public async Task Sucesso_ProcessaUmaVezEConfirma()
    {
        var fila = Rabbit.NomeUnico("ok");
        var manipulador = ManipuladorRoteirizado.SempreOk();
        await using var consumidor = await IniciarAsync(infra, fila, Politica, manipulador);

        await Rabbit.PublicarAsync(infra.Rabbit, fila, """{"pedido":1}""", "m-ok");

        await Esperar.Eventualmente(() => manipulador.Sucessos.Count == 1, "processar a mensagem");
        manipulador.TotalDeChamadas.ShouldBe(1);
        manipulador.Sucessos.Single().TentativasAtrasadas.ShouldBe(0);
        (await Rabbit.ContarAsync(infra.Rabbit, PoliticaDeRetry.NomeDaDlq(fila))).ShouldBe(0u);
    }

    [Fact]
    public async Task FalhaTransitoriaPassageira_RecuperaNoRetryImediato()
    {
        var fila = Rabbit.NomeUnico("imediato");
        // Falha nas 2 primeiras chamadas (deadlock, timeout...), funciona na 3ª.
        var manipulador = new ManipuladorRoteirizado((n, _) => n <= 2 ? new TimeoutException($"soluço {n}") : null);
        await using var consumidor = await IniciarAsync(infra, fila, Politica, manipulador);

        await Rabbit.PublicarAsync(infra.Rabbit, fila, "{}", "m-imediato");

        await Esperar.Eventualmente(() => manipulador.Sucessos.Count == 1, "recuperar no retry imediato");
        manipulador.TotalDeChamadas.ShouldBe(3);
        var sucesso = manipulador.Sucessos.Single();
        sucesso.TentativaImediata.ShouldBe(2);
        sucesso.TentativasAtrasadas.ShouldBe(0, "não precisou passar pela fila de espera");
    }

    [Fact]
    public async Task FalhaTransitoriaPersistente_PassaPelaFilaDeEsperaEVolta()
    {
        var fila = Rabbit.NomeUnico("atrasado");
        // As 3 execuções da primeira entrega falham (1 + 2 imediatas); a da volta funciona.
        var manipulador = new ManipuladorRoteirizado((n, _) => n <= 3 ? new ErroTransitorioException("estoque reiniciando") : null);
        await using var consumidor = await IniciarAsync(infra, fila, Politica, manipulador);

        var relogio = Stopwatch.StartNew();
        await Rabbit.PublicarAsync(infra.Rabbit, fila, """{"pedido":2}""", "m-atrasado");

        await Esperar.Eventualmente(() => manipulador.Sucessos.Count == 1, "processar depois da fila de espera");
        relogio.Elapsed.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(150), "esperou o TTL do nível 1 (200 ms)");
        manipulador.TotalDeChamadas.ShouldBe(4);

        var sucesso = manipulador.Sucessos.Single();
        sucesso.TentativasAtrasadas.ShouldBe(1);
        sucesso.MessageId.ShouldBe("m-atrasado");
        sucesso.CorpoComoTexto.ShouldBe("""{"pedido":2}""");
        (await Rabbit.ContarAsync(infra.Rabbit, PoliticaDeRetry.NomeDaDlq(fila))).ShouldBe(0u);
    }

    [Fact]
    public async Task ErroPermanente_VaiDiretoParaDlqComMotivoSemRetentar()
    {
        var fila = Rabbit.NomeUnico("permanente");
        var manipulador = new ManipuladorRoteirizado((_, _) => new ErroPermanenteException("cliente 42 não existe"));
        await using var consumidor = await IniciarAsync(infra, fila, Politica, manipulador);

        await Rabbit.PublicarAsync(infra.Rabbit, fila, """{"cliente":42}""", "m-permanente", "PedidoCriado");

        var dlq = PoliticaDeRetry.NomeDaDlq(fila);
        await Esperar.Eventualmente(async () => await Rabbit.ContarAsync(infra.Rabbit, dlq) == 1, "a mensagem chegar à DLQ");
        manipulador.TotalDeChamadas.ShouldBe(1, "erro permanente não se repete");

        var morta = (await Rabbit.DrenarAsync(infra.Rabbit, dlq)).ShouldHaveSingleItem();
        morta.MessageId.ShouldBe("m-permanente");
        morta.Tipo.ShouldBe("PedidoCriado");
        morta.CorpoComoTexto.ShouldBe("""{"cliente":42}""");
        Cabecalhos.LerTexto(morta.Cabecalhos, Cabecalhos.Motivo).ShouldBe(nameof(MotivoDeadLetter.ErroPermanente));
        Cabecalhos.LerTexto(morta.Cabecalhos, Cabecalhos.Erro).ShouldNotBeNull().ShouldContain("cliente 42 não existe");
        Cabecalhos.LerTexto(morta.Cabecalhos, Cabecalhos.FilaDeOrigem).ShouldBe(fila);
        Cabecalhos.LerTexto(morta.Cabecalhos, Cabecalhos.FalhouEm).ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CorpoInvalido_VaiParaDlqSemRetentar()
    {
        var fila = Rabbit.NomeUnico("json");
        var manipulador = new ManipuladorRoteirizado((_, m) =>
        {
            m.Ler<Dictionary<string, int>>(); // lança JsonException para corpo inválido
            return null;
        });
        await using var consumidor = await IniciarAsync(infra, fila, Politica, manipulador);

        await Rabbit.PublicarAsync(infra.Rabbit, fila, "{ isto não é json", "m-lixo");

        var dlq = PoliticaDeRetry.NomeDaDlq(fila);
        await Esperar.Eventualmente(async () => await Rabbit.ContarAsync(infra.Rabbit, dlq) == 1, "o lixo chegar à DLQ");
        manipulador.TotalDeChamadas.ShouldBe(1);
        var morta = (await Rabbit.DrenarAsync(infra.Rabbit, dlq)).ShouldHaveSingleItem();
        Cabecalhos.LerTexto(morta.Cabecalhos, Cabecalhos.Motivo).ShouldBe(nameof(MotivoDeadLetter.ErroPermanente));
    }

    [Fact]
    public async Task MensagemVenenosa_VaiParaDlqDepoisDeTodasAsTentativas()
    {
        var fila = Rabbit.NomeUnico("veneno");
        // Bug determinístico que o classificador não reconhece: transitório "por via das dúvidas".
        var manipulador = new ManipuladorRoteirizado((_, _) => new InvalidOperationException("NullReference disfarçada"));
        await using var consumidor = await IniciarAsync(infra, fila, Politica, manipulador);

        await Rabbit.PublicarAsync(infra.Rabbit, fila, """{"pedido":"veneno"}""", "m-veneno");

        var dlq = PoliticaDeRetry.NomeDaDlq(fila);
        await Esperar.Eventualmente(async () => await Rabbit.ContarAsync(infra.Rabbit, dlq) == 1, "a venenosa chegar à DLQ");
        manipulador.TotalDeChamadas.ShouldBe(Politica.TotalDeExecucoes); // 9
        manipulador.Chamadas.Select(c => c.TentativasAtrasadas).Distinct().Order().ShouldBe([0, 1, 2]);

        var morta = (await Rabbit.DrenarAsync(infra.Rabbit, dlq)).ShouldHaveSingleItem();
        morta.MessageId.ShouldBe("m-veneno");
        Cabecalhos.LerTexto(morta.Cabecalhos, Cabecalhos.Motivo).ShouldBe(nameof(MotivoDeadLetter.RetentativasEsgotadas));
        Cabecalhos.LerTexto(morta.Cabecalhos, Cabecalhos.Erro).ShouldNotBeNull().ShouldContain(nameof(InvalidOperationException));
        morta.TentativasAtrasadas.ShouldBe(2);
        (await Rabbit.ContarAsync(infra.Rabbit, fila)).ShouldBe(0u, "nada fica girando na fila principal");
    }
}
