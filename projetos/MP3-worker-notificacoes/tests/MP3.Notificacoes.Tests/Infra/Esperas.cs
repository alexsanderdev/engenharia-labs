using System.Diagnostics;

namespace MP3.Notificacoes.Tests.Infra;

/// <summary>
/// Esperas para testes com broker: NUNCA "Task.Delay(2000) e torcer".
/// Ou o teste espera um sinal (<see cref="TaskCompletionSource"/>), ou faz polling com timeout curto,
/// ou usa uma leitura com timeout (o próprio <c>ReceiveMessagesAsync</c> espera até chegar algo).
/// </summary>
public static class Esperas
{
    /// <summary>Folgado para máquina lenta, curto para a suíte não travar.</summary>
    public static readonly TimeSpan Padrao = TimeSpan.FromSeconds(20);

    /// <summary>Repete a verificação até passar ou o tempo acabar (polling a cada 100 ms).</summary>
    public static async Task Eventualmente(Func<Task<bool>> condicao, string descricao, TimeSpan? timeout = null)
    {
        var limite = timeout ?? Padrao;
        var relogio = Stopwatch.StartNew();
        while (relogio.Elapsed < limite)
        {
            if (await condicao()) return;
            await Task.Delay(100);
        }
        throw new TimeoutException($"Esperei {limite.TotalSeconds:0} s e não aconteceu: {descricao}");
    }

    public static Task Eventualmente(Func<bool> condicao, string descricao, TimeSpan? timeout = null) =>
        Eventualmente(() => Task.FromResult(condicao()), descricao, timeout);

    /// <summary>Espera um sinal com timeout e mensagem clara se ele não vier.</summary>
    public static async Task Sinal(Task sinal, string descricao, TimeSpan? timeout = null)
    {
        try
        {
            await sinal.WaitAsync(timeout ?? Padrao);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Sinal não chegou em {(timeout ?? Padrao).TotalSeconds:0} s: {descricao}");
        }
    }
}
