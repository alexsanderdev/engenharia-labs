using System.Collections.Concurrent;

namespace F6M06.Worker.Saude;

/// <summary>Fotografia do estado de um worker. (PRONTO)</summary>
/// <param name="UltimoBatimento">Fim do último ciclo (com ou sem sucesso): prova de que o loop está vivo.</param>
/// <param name="FalhasConsecutivas">Ciclos seguidos que falharam (zera no primeiro sucesso).</param>
/// <param name="FalhaFatal">Exceção que matou o worker, se houver.</param>
public sealed record EstadoDoWorker(DateTimeOffset? UltimoBatimento, int FalhasConsecutivas, Exception? FalhaFatal);

/// <summary>
/// Onde os workers registram batimentos e falhas, e de onde o health check e o <c>Program</c> leem.
/// SINGLETON, thread-safe. (PRONTO)
/// </summary>
public sealed class MonitorDeWorkers(TimeProvider relogio)
{
    private readonly ConcurrentDictionary<string, EstadoDoWorker> _estados = new();

    /// <summary>Registra o fim de um ciclo (batimento).</summary>
    public void RegistrarCiclo(string worker, bool sucesso) =>
        _estados.AddOrUpdate(
            worker,
            _ => new EstadoDoWorker(relogio.GetUtcNow(), sucesso ? 0 : 1, null),
            (_, atual) => atual with
            {
                UltimoBatimento = relogio.GetUtcNow(),
                FalhasConsecutivas = sucesso ? 0 : atual.FalhasConsecutivas + 1,
            });

    /// <summary>Registra que o worker morreu com <paramref name="erro"/>.</summary>
    public void RegistrarFalhaFatal(string worker, Exception erro) =>
        _estados.AddOrUpdate(
            worker,
            _ => new EstadoDoWorker(null, 0, erro),
            (_, atual) => atual with { FalhaFatal = erro });

    /// <summary>Estado atual (vazio se o worker nunca bateu).</summary>
    public EstadoDoWorker Obter(string worker) =>
        _estados.TryGetValue(worker, out var estado) ? estado : new EstadoDoWorker(null, 0, null);

    /// <summary>Algum worker morreu? (o <c>Program</c> usa para devolver exit code ≠ 0)</summary>
    public bool AlgumaFalhaFatal => _estados.Values.Any(e => e.FalhaFatal is not null);
}
