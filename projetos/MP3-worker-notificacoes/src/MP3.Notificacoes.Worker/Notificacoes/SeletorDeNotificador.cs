using Microsoft.Extensions.Options;
using MP3.Notificacoes.Worker.Configuracao;

namespace MP3.Notificacoes.Worker.Notificacoes;

/// <summary>
/// Contexto do Strategy: escolhe o <see cref="INotificador"/> pelo canal preferido do cliente,
/// caindo para o canal padrão da configuração. Novo canal = nova classe + uma linha de DI; nada aqui muda.
/// </summary>
public sealed class SeletorDeNotificador
{
    private readonly Dictionary<string, INotificador> porCanal;
    private readonly INotificador padrao;

    public SeletorDeNotificador(IEnumerable<INotificador> notificadores, IOptions<NotificacoesOptions> opcoes)
    {
        ArgumentNullException.ThrowIfNull(notificadores);
        ArgumentNullException.ThrowIfNull(opcoes);
        porCanal = new(StringComparer.OrdinalIgnoreCase);
        foreach (var n in notificadores) porCanal[n.Canal] = n; // o último registrado vence (útil para testes)

        padrao = porCanal.GetValueOrDefault(opcoes.Value.CanalPadrao)
            ?? throw new InvalidOperationException(
                $"Canal padrão '{opcoes.Value.CanalPadrao}' não tem notificador registrado. Registrados: {string.Join(", ", porCanal.Keys)}.");
    }

    public IReadOnlyCollection<string> Canais => porCanal.Keys;

    public INotificador Selecionar(string? canalPreferido) =>
        canalPreferido is not null && porCanal.TryGetValue(canalPreferido, out var escolhido) ? escolhido : padrao;
}
