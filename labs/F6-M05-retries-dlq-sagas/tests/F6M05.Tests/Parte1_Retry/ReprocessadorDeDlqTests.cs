using F6M05.Sagas.Retry;
using F6M05.Tests.Infra;

namespace F6M05.Tests.Parte1_Retry;

/// <summary>Passo 4 — operar a DLQ: listar sem perder e reprocessar depois da correção.</summary>
public sealed class ReprocessadorDeDlqTests(InfraFixture infra)
{
    /// <summary>Política curta: (1+1) × (1+1) = 4 execuções antes da DLQ.</summary>
    private static readonly PoliticaDeRetry Politica = new()
    {
        RetentativasImediatas = 1,
        Atrasos = [TimeSpan.FromMilliseconds(100)],
    };

    [Fact]
    public async Task Reprocessar_DepoisDaCorrecao_AMensagemVoltaParaAFilaEEProcessada()
    {
        var fila = Rabbit.NomeUnico("reprocessar");
        var corrigido = false;
        var manipulador = new ManipuladorRoteirizado((_, _) => Volatile.Read(ref corrigido) ? null : new InvalidOperationException("bug no cálculo do frete"));
        await using var consumidor = await ConsumidorComRetryTests.IniciarAsync(infra, fila, Politica, manipulador);
        var dlq = PoliticaDeRetry.NomeDaDlq(fila);

        await Rabbit.PublicarAsync(infra.Rabbit, fila, """{"pedido":7}""", "m-frete", "PedidoCriado");
        await Esperar.Eventualmente(async () => await Rabbit.ContarAsync(infra.Rabbit, dlq) == 1, "a venenosa chegar à DLQ");
        manipulador.TotalDeChamadas.ShouldBe(Politica.TotalDeExecucoes);

        // "Deploy da correção" e reprocessamento.
        Volatile.Write(ref corrigido, true);
        var resultado = await new ReprocessadorDeDlq(infra.Rabbit).ReprocessarAsync(fila);

        resultado.ShouldBe(new ResultadoReprocessamento(Movidas: 1, Mantidas: 0));
        await Esperar.Eventualmente(() => manipulador.Sucessos.Count == 1, "processar a mensagem reprocessada");
        var processada = manipulador.Sucessos.Single();
        processada.MessageId.ShouldBe("m-frete", "o MessageId é preservado: a idempotência continua valendo");
        processada.Tipo.ShouldBe("PedidoCriado");
        processada.CorpoComoTexto.ShouldBe("""{"pedido":7}""");
        processada.TentativasAtrasadas.ShouldBe(0, "o contador de tentativas recomeça");
        processada.Reprocessamentos.ShouldBe(1);
        (await Rabbit.ContarAsync(infra.Rabbit, dlq)).ShouldBe(0u);
    }

    [Fact]
    public async Task Listar_MostraMotivoETentativas_SemRemoverDaDlq()
    {
        var fila = Rabbit.NomeUnico("listar");
        var manipulador = new ManipuladorRoteirizado((_, m) => new ErroPermanenteException($"produto inativo em {m.MessageId}"));
        await using var consumidor = await ConsumidorComRetryTests.IniciarAsync(infra, fila, Politica, manipulador);
        var dlq = PoliticaDeRetry.NomeDaDlq(fila);

        await Rabbit.PublicarAsync(infra.Rabbit, fila, "{}", "m-1");
        await Rabbit.PublicarAsync(infra.Rabbit, fila, "{}", "m-2");
        await Esperar.Eventualmente(async () => await Rabbit.ContarAsync(infra.Rabbit, dlq) == 2, "as duas chegarem à DLQ");

        var reprocessador = new ReprocessadorDeDlq(infra.Rabbit);
        var listadas = await reprocessador.ListarAsync(fila);

        listadas.Select(m => m.MessageId).Order().ShouldBe(["m-1", "m-2"]);
        listadas.ShouldAllBe(m => m.Motivo == nameof(Sagas.Mensageria.MotivoDeadLetter.ErroPermanente));
        listadas.ShouldAllBe(m => m.FilaDeOrigem == fila);
        listadas.Single(m => m.MessageId == "m-1").Erro.ShouldNotBeNull().ShouldContain("produto inativo em m-1");
        (await Rabbit.ContarAsync(infra.Rabbit, dlq)).ShouldBe(2u, "listar não pode consumir a DLQ");
    }

    [Fact]
    public async Task Reprocessar_ComFiltro_MoveSoAsSelecionadas()
    {
        var fila = Rabbit.NomeUnico("filtro");
        var liberados = new HashSet<string>();
        var manipulador = new ManipuladorRoteirizado((_, m) =>
        {
            lock (liberados)
                return liberados.Contains(m.MessageId) ? null : new ErroPermanenteException("cadastro faltando");
        });
        await using var consumidor = await ConsumidorComRetryTests.IniciarAsync(infra, fila, Politica, manipulador);
        var dlq = PoliticaDeRetry.NomeDaDlq(fila);

        await Rabbit.PublicarAsync(infra.Rabbit, fila, "{}", "m-corrigida");
        await Rabbit.PublicarAsync(infra.Rabbit, fila, "{}", "m-ainda-quebrada");
        await Esperar.Eventualmente(async () => await Rabbit.ContarAsync(infra.Rabbit, dlq) == 2, "as duas chegarem à DLQ");

        lock (liberados) liberados.Add("m-corrigida");
        var resultado = await new ReprocessadorDeDlq(infra.Rabbit).ReprocessarAsync(fila, m => m.MessageId == "m-corrigida");

        resultado.ShouldBe(new ResultadoReprocessamento(Movidas: 1, Mantidas: 1));
        await Esperar.Eventualmente(() => manipulador.Sucessos.Count == 1, "processar a corrigida");
        manipulador.Sucessos.Single().MessageId.ShouldBe("m-corrigida");
        var restantes = await new ReprocessadorDeDlq(infra.Rabbit).ListarAsync(fila);
        restantes.ShouldHaveSingleItem().MessageId.ShouldBe("m-ainda-quebrada");
    }
}
