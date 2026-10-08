using Microsoft.Extensions.Options;

namespace F4M07.Patterns.Notificacoes;

/// <summary>
/// FACTORY com regra de criação de verdade (não um <c>new</c> embrulhado): valida o contato exigido por canal,
/// formata o valor em pt-BR, respeita o limite do SMS e concentra textos/remetente vindos de configuração.
/// </summary>
public sealed class FabricaDeNotificacoes(IOptions<OpcoesDeNotificacao> opcoes) : IFabricaDeNotificacoes
{
    /// <summary>
    /// <list type="bullet">
    /// <item>E-mail: exige <c>Email</c>; assunto "Pedido {numero} confirmado"; corpo começa com "Olá, {Nome}!" e contém
    /// "R$ {total:N2 pt-BR}" (ex.: "R$ 1.234,50"); remetente = <see cref="OpcoesDeNotificacao.RemetenteDeEmail"/>.</item>
    /// <item>SMS: exige <c>Telefone</c>; texto "{NomeDaLoja}: pedido {numero} confirmado. Total R$ {total}"; se passar de
    /// 160 caracteres, corta em 157 e termina com "...".</item>
    /// <item>Push: exige <c>TokenPush</c>; título "Pedido confirmado"; mensagem "Seu pedido {numero} foi confirmado.".</item>
    /// <item>Dado ausente/em branco: <see cref="CanalIndisponivelException"/>.</item>
    /// </list>
    /// </summary>
    public Notificacao Criar(CanalDeNotificacao canal, ClienteNotificavel cliente, PedidoConfirmado pedido) =>
        throw new NotImplementedException(
            "TODO: switch no canal; total = \"R$ \" + pedido.Total.ToString(\"N2\", CultureInfo.GetCultureInfo(\"pt-BR\")); " +
            $"textos e remetente vêm de opcoes.Value ({opcoes.GetType().Name}).");

    /// <summary>
    /// Uma notificação por canal em <see cref="ClienteNotificavel.CanaisPreferidos"/>, na ordem do enum
    /// (E-mail, SMS, Push), PULANDO os canais sem dado de contato (sem lançar).
    /// </summary>
    public IReadOnlyList<Notificacao> CriarParaPreferencias(ClienteNotificavel cliente, PedidoConfirmado pedido) =>
        throw new NotImplementedException("TODO: percorra Enum.GetValues<CanalDeNotificacao>(), filtre pelas preferências e reaproveite Criar.");
}
