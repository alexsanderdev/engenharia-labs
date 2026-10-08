using System.Globalization;
using Microsoft.Extensions.Options;

namespace F4M07.Patterns.Notificacoes;

/// <summary>
/// FACTORY com regra de criação de verdade (não um <c>new</c> embrulhado): valida o contato exigido por canal,
/// formata o valor em pt-BR, respeita o limite do SMS e concentra textos/remetente vindos de configuração.
/// </summary>
public sealed class FabricaDeNotificacoes(IOptions<OpcoesDeNotificacao> opcoes) : IFabricaDeNotificacoes
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

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
    public Notificacao Criar(CanalDeNotificacao canal, ClienteNotificavel cliente, PedidoConfirmado pedido)
    {
        var total = "R$ " + pedido.Total.ToString("N2", PtBr);
        return canal switch
        {
            CanalDeNotificacao.Email => new NotificacaoPorEmail(
                opcoes.Value.RemetenteDeEmail,
                Exigir(cliente.Email, canal, "cliente sem e-mail"),
                $"Pedido {pedido.NumeroDoPedido} confirmado",
                $"Olá, {cliente.Nome}! Seu pedido {pedido.NumeroDoPedido} foi confirmado. Total: {total}."),
            CanalDeNotificacao.Sms => new NotificacaoPorSms(
                Exigir(cliente.Telefone, canal, "cliente sem telefone"),
                Limitar($"{opcoes.Value.NomeDaLoja}: pedido {pedido.NumeroDoPedido} confirmado. Total {total}")),
            CanalDeNotificacao.Push => new NotificacaoPush(
                Exigir(cliente.TokenPush, canal, "cliente sem dispositivo cadastrado"),
                "Pedido confirmado",
                $"Seu pedido {pedido.NumeroDoPedido} foi confirmado."),
            _ => throw new ArgumentOutOfRangeException(nameof(canal), canal, "Canal não suportado."),
        };
    }

    /// <summary>
    /// Uma notificação por canal em <see cref="ClienteNotificavel.CanaisPreferidos"/>, na ordem do enum
    /// (E-mail, SMS, Push), PULANDO os canais sem dado de contato (sem lançar).
    /// </summary>
    public IReadOnlyList<Notificacao> CriarParaPreferencias(ClienteNotificavel cliente, PedidoConfirmado pedido)
    {
        var notificacoes = new List<Notificacao>();
        foreach (var canal in Enum.GetValues<CanalDeNotificacao>())
        {
            if (!cliente.CanaisPreferidos.Contains(canal))
                continue;
            try
            {
                notificacoes.Add(Criar(canal, cliente, pedido));
            }
            catch (CanalIndisponivelException)
            {
                // Preferência sem contato: pula. Em produção, logue/meça isso.
            }
        }

        return notificacoes;
    }

    private static string Exigir(string? valor, CanalDeNotificacao canal, string motivo) =>
        string.IsNullOrWhiteSpace(valor) ? throw new CanalIndisponivelException(canal, motivo) : valor;

    private static string Limitar(string texto) =>
        texto.Length <= NotificacaoPorSms.LimiteDeCaracteres
            ? texto
            : string.Concat(texto.AsSpan(0, NotificacaoPorSms.LimiteDeCaracteres - 3), "...");
}
