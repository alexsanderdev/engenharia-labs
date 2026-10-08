using F4M07.Patterns.Notificacoes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace F4M07.Patterns.Tests;

/// <summary>Passo 4 — FACTORY: criar a notificação certa por canal, com as regras de criação num lugar só.</summary>
public sealed class FactoryNotificacaoTests
{
    private static readonly PedidoConfirmado Pedido = new("PED-2026-0042", 1_234.5m);

    private static ClienteNotificavel Ana(params CanalDeNotificacao[] canais) =>
        new("Ana", "ana@exemplo.com", "+5511999990000", "device-123", canais.ToHashSet());

    private static FabricaDeNotificacoes CriarFabrica(string loja = "OrderFlow") =>
        new(Options.Create(new OpcoesDeNotificacao { NomeDaLoja = loja, RemetenteDeEmail = "pedidos@orderflow.dev" }));

    [Fact]
    public void Criar_Email_PreencheRemetenteAssuntoEValorEmPtBr()
    {
        var email = CriarFabrica().Criar(CanalDeNotificacao.Email, Ana(), Pedido).ShouldBeOfType<NotificacaoPorEmail>();

        email.Remetente.ShouldBe("pedidos@orderflow.dev");
        email.Para.ShouldBe("ana@exemplo.com");
        email.Assunto.ShouldBe("Pedido PED-2026-0042 confirmado");
        email.Corpo.ShouldStartWith("Olá, Ana!");
        email.Corpo.ShouldContain("R$ 1.234,50");
        email.Canal.ShouldBe(CanalDeNotificacao.Email);
    }

    [Fact]
    public void Criar_Sms_TextoCurtoComNomeDaLoja()
    {
        var sms = CriarFabrica().Criar(CanalDeNotificacao.Sms, Ana(), Pedido).ShouldBeOfType<NotificacaoPorSms>();

        sms.Telefone.ShouldBe("+5511999990000");
        sms.Texto.ShouldBe("OrderFlow: pedido PED-2026-0042 confirmado. Total R$ 1.234,50");
    }

    [Fact]
    public void Criar_SmsQuePassariaDe160Caracteres_CortaETerminaComReticencias()
    {
        var sms = CriarFabrica(loja: new string('X', 150)).Criar(CanalDeNotificacao.Sms, Ana(), Pedido)
            .ShouldBeOfType<NotificacaoPorSms>();

        sms.Texto.Length.ShouldBe(160);
        sms.Texto.ShouldEndWith("...");
    }

    [Fact]
    public void Criar_Push_UsaTokenDoDispositivo()
    {
        var push = CriarFabrica().Criar(CanalDeNotificacao.Push, Ana(), Pedido).ShouldBeOfType<NotificacaoPush>();

        push.ShouldBe(new NotificacaoPush("device-123", "Pedido confirmado", "Seu pedido PED-2026-0042 foi confirmado."));
    }

    [Theory]
    [InlineData(CanalDeNotificacao.Email)]
    [InlineData(CanalDeNotificacao.Sms)]
    [InlineData(CanalDeNotificacao.Push)]
    public void Criar_ClienteSemODadoDeContatoDoCanal_LancaCanalIndisponivel(CanalDeNotificacao canal)
    {
        var semContato = new ClienteNotificavel("Bia", Email: " ", Telefone: null, TokenPush: "", new HashSet<CanalDeNotificacao>());

        Should.Throw<CanalIndisponivelException>(() => CriarFabrica().Criar(canal, semContato, Pedido)).Canal.ShouldBe(canal);
    }

    [Fact]
    public void CriarParaPreferencias_RespeitaPreferenciasOrdemDoEnumEPulaCanalSemContato()
    {
        var cliente = Ana(CanalDeNotificacao.Push, CanalDeNotificacao.Email, CanalDeNotificacao.Sms) with { Telefone = null };

        var notificacoes = CriarFabrica().CriarParaPreferencias(cliente, Pedido);

        notificacoes.Select(n => n.Canal).ShouldBe([CanalDeNotificacao.Email, CanalDeNotificacao.Push]);
    }

    [Fact]
    public void ViaDI_FabricaUsaAsOpcoesConfiguradas()
    {
        using var provider = Composicao.CriarProvider(services =>
            services.Configure<OpcoesDeNotificacao>(o => o.NomeDaLoja = "Café do Zé"));

        var sms = provider.GetRequiredService<IFabricaDeNotificacoes>()
            .Criar(CanalDeNotificacao.Sms, Ana(), Pedido).ShouldBeOfType<NotificacaoPorSms>();

        sms.Texto.ShouldStartWith("Café do Zé:");
    }
}
