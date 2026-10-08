using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MP3.Notificacoes.Tests.Infra;
using MP3.Notificacoes.Worker.Configuracao;
using MP3.Notificacoes.Worker.Mensageria;
using MP3.Notificacoes.Worker.Notificacoes;

namespace MP3.Notificacoes.Tests;

/// <summary>Peças puras, sem containers: backoff, Strategy e provedores.</summary>
public sealed class UnidadeTests
{
    private static IOptions<NotificacoesOptions> Opcoes(string canalPadrao = "console", double jitter = 0) => Options.Create(new NotificacoesOptions
    {
        CanalPadrao = canalPadrao,
        Retry = new RetryOptions
        {
            MaxTentativas = 5,
            AtrasoBase = TimeSpan.FromSeconds(1),
            AtrasoMaximo = TimeSpan.FromSeconds(10),
            Jitter = jitter,
        },
    });

    [Fact]
    public void Backoff_DobraACadaTentativa_ERespeitaOTeto()
    {
        var politica = new PoliticaDeRetry(Opcoes());

        politica.AtrasoApos(1).ShouldBe(TimeSpan.FromSeconds(1));
        politica.AtrasoApos(2).ShouldBe(TimeSpan.FromSeconds(2));
        politica.AtrasoApos(4).ShouldBe(TimeSpan.FromSeconds(8));
        politica.AtrasoApos(5).ShouldBe(TimeSpan.FromSeconds(10), "teto");
        politica.DeveTentarDeNovo(4).ShouldBeTrue();
        politica.DeveTentarDeNovo(5).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0.0, 800)]
    [InlineData(0.5, 1000)]
    [InlineData(1.0, 1200)]
    public void Backoff_JitterFicaDentroDaFaixa(double sorteio, int esperadoMs)
    {
        var politica = new PoliticaDeRetry(Opcoes(jitter: 0.2), () => sorteio);

        politica.AtrasoApos(1).TotalMilliseconds.ShouldBe(esperadoMs, tolerance: 0.001);
    }

    [Fact]
    public void Seletor_UsaOCanalPreferido_OuCaiNoPadrao()
    {
        INotificador[] notificadores = [new NotificadorConsole(TextWriter.Null), new NotificadorSmsFake(NullLogger<NotificadorSmsFake>.Instance)];
        var seletor = new SeletorDeNotificador(notificadores, Opcoes("console"));

        seletor.Selecionar("SMS").Canal.ShouldBe("sms");
        seletor.Selecionar(null).Canal.ShouldBe("console");
        seletor.Selecionar("pombo-correio").Canal.ShouldBe("console");
        Should.Throw<InvalidOperationException>(() => new SeletorDeNotificador(notificadores, Opcoes("email")));
    }

    [Fact]
    public void Modelo_CanalSemDestino_EFalhaPermanente()
    {
        var semEmail = Novo.Pedido() with { Email = null };

        Should.Throw<FalhaPermanenteException>(() => ModeloDeMensagem.Para(semEmail, "email"));
        ModeloDeMensagem.Para(Novo.Pedido(), "sms").Destino.ShouldBe("+5511912345678");
        ModeloDeMensagem.Para(Novo.Pedido(valor: 1234.5m), "console").Mensagem.ShouldContain("1.234,50");
    }

    [Fact]
    public async Task Email_ModoPastaDeSaida_GravaEmlComChaveDeIdempotencia()
    {
        var pasta = Path.Combine(Path.GetTempPath(), $"mp3-emails-{Guid.NewGuid():N}");
        try
        {
            var email = new NotificadorEmailSmtp(Options.Create(new SmtpOptions { Modo = ModoSmtp.PastaDeSaida, PastaDeSaida = pasta }));
            var notificacao = ModeloDeMensagem.Para(Novo.Pedido(), "email");

            await email.EnviarAsync(notificacao, TestContext.Current.CancellationToken);

            var eml = await File.ReadAllTextAsync(Directory.GetFiles(pasta, "*.eml").Single(), TestContext.Current.CancellationToken);
            eml.ShouldContain("To: ana@example.com");
            eml.ShouldContain($"{NotificadorEmailSmtp.CabecalhoDeIdempotencia}: {notificacao.EventoId}");
        }
        finally
        {
            if (Directory.Exists(pasta)) Directory.Delete(pasta, recursive: true);
        }
    }

    [Fact]
    public async Task EmailInvalido_E_TelefoneForaDoE164_SaoFalhasPermanentes()
    {
        var email = new NotificadorEmailSmtp(Options.Create(new SmtpOptions { PastaDeSaida = Path.GetTempPath() }));
        var sms = new NotificadorSmsFake(NullLogger<NotificadorSmsFake>.Instance);
        var pedido = Novo.Pedido() with { Email = "isso não é email", Telefone = "11 91234-5678" };

        await Should.ThrowAsync<FalhaPermanenteException>(() => email.EnviarAsync(ModeloDeMensagem.Para(pedido, "email"), CancellationToken.None));
        await Should.ThrowAsync<FalhaPermanenteException>(() => sms.EnviarAsync(ModeloDeMensagem.Para(pedido, "sms"), CancellationToken.None));
        sms.Enviados.ShouldBeEmpty();
    }
}
