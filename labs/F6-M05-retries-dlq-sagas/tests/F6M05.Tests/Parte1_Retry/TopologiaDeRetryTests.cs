using F6M05.Sagas.Retry;
using F6M05.Tests.Infra;

namespace F6M05.Tests.Parte1_Retry;

/// <summary>Passo 2 — a topologia de espera com TTL + dead-letter exchange.</summary>
public sealed class TopologiaDeRetryTests(InfraFixture infra)
{
    private static readonly PoliticaDeRetry Politica = new()
    {
        RetentativasImediatas = 1,
        Atrasos = [TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(400)],
    };

    [Fact]
    public async Task Declarar_CriaFilaPrincipalFilasDeEsperaEDlq_EEIdempotente()
    {
        var fila = Rabbit.NomeUnico("topologia");
        await using var canal = await infra.Rabbit.CreateChannelAsync();

        await TopologiaDeRetry.DeclararAsync(canal, fila, Politica);
        await TopologiaDeRetry.DeclararAsync(canal, fila, Politica); // declarar de novo não pode falhar

        (await Rabbit.FilaExisteAsync(infra.Rabbit, fila)).ShouldBeTrue("fila principal");
        (await Rabbit.FilaExisteAsync(infra.Rabbit, PoliticaDeRetry.NomeDaFilaDeEspera(fila, 1))).ShouldBeTrue("espera nível 1");
        (await Rabbit.FilaExisteAsync(infra.Rabbit, PoliticaDeRetry.NomeDaFilaDeEspera(fila, 2))).ShouldBeTrue("espera nível 2");
        (await Rabbit.FilaExisteAsync(infra.Rabbit, PoliticaDeRetry.NomeDaFilaDeEspera(fila, 3))).ShouldBeFalse("só há 2 atrasos");
        (await Rabbit.FilaExisteAsync(infra.Rabbit, PoliticaDeRetry.NomeDaDlq(fila))).ShouldBeTrue("DLQ");
    }

    [Fact]
    public async Task FilaDeEspera_QuandoOTtlVence_DevolveAMensagemParaAFilaPrincipal()
    {
        var fila = Rabbit.NomeUnico("ttl");
        await using (var canal = await infra.Rabbit.CreateChannelAsync())
            await TopologiaDeRetry.DeclararAsync(canal, fila, Politica);

        await Rabbit.PublicarAsync(infra.Rabbit, PoliticaDeRetry.NomeDaFilaDeEspera(fila, 2), """{"ok":true}""", "msg-ttl");

        // Ninguém consome a fila de espera: quem move a mensagem é o broker (TTL → DLX → fila principal).
        await Esperar.Eventualmente(async () => await Rabbit.ContarAsync(infra.Rabbit, fila) == 1,
            "a mensagem voltar da fila de espera para a principal");

        var voltou = (await Rabbit.DrenarAsync(infra.Rabbit, fila)).ShouldHaveSingleItem();
        voltou.MessageId.ShouldBe("msg-ttl");
        voltou.CorpoComoTexto.ShouldBe("""{"ok":true}""");
    }
}
