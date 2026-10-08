namespace F4M07.Patterns.Notificacoes;

/// <summary>Canais suportados. A ordem do enum é a ordem de envio.</summary>
public enum CanalDeNotificacao
{
    Email,
    Sms,
    Push,
}

/// <summary>Dados de contato do cliente e canais que ele aceitou receber.</summary>
public sealed record ClienteNotificavel(
    string Nome,
    string? Email,
    string? Telefone,
    string? TokenPush,
    IReadOnlySet<CanalDeNotificacao> CanaisPreferidos);

/// <summary>Evento de domínio que dispara a notificação.</summary>
public sealed record PedidoConfirmado(string NumeroDoPedido, decimal Total);

/// <summary>Notificação pronta para um canal. Cada tipo concreto tem os campos que o seu canal exige.</summary>
public abstract record Notificacao
{
    public abstract CanalDeNotificacao Canal { get; }
}

public sealed record NotificacaoPorEmail(string Remetente, string Para, string Assunto, string Corpo) : Notificacao
{
    public override CanalDeNotificacao Canal => CanalDeNotificacao.Email;
}

public sealed record NotificacaoPorSms(string Telefone, string Texto) : Notificacao
{
    /// <summary>Limite de um SMS simples.</summary>
    public const int LimiteDeCaracteres = 160;

    public override CanalDeNotificacao Canal => CanalDeNotificacao.Sms;
}

public sealed record NotificacaoPush(string Token, string Titulo, string Mensagem) : Notificacao
{
    public override CanalDeNotificacao Canal => CanalDeNotificacao.Push;
}

/// <summary>O cliente não tem o dado de contato que o canal exige.</summary>
public sealed class CanalIndisponivelException(CanalDeNotificacao canal, string motivo)
    : Exception($"Não é possível notificar por {canal}: {motivo}.")
{
    public CanalDeNotificacao Canal { get; } = canal;
}

/// <summary>Opções de notificação (seção "Notificacoes").</summary>
public sealed class OpcoesDeNotificacao
{
    public string NomeDaLoja { get; set; } = "OrderFlow";

    public string RemetenteDeEmail { get; set; } = "nao-responda@orderflow.dev";
}

/// <summary>FACTORY: única porta para criar notificações. Quem usa não faz <c>new NotificacaoPorSms(...)</c>.</summary>
public interface IFabricaDeNotificacoes
{
    /// <summary>Cria a notificação de "pedido confirmado" para um canal específico.</summary>
    Notificacao Criar(CanalDeNotificacao canal, ClienteNotificavel cliente, PedidoConfirmado pedido);

    /// <summary>Cria uma notificação por canal preferido do cliente, pulando canais sem dado de contato.</summary>
    IReadOnlyList<Notificacao> CriarParaPreferencias(ClienteNotificavel cliente, PedidoConfirmado pedido);
}
