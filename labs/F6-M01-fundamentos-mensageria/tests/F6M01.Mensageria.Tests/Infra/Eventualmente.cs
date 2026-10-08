namespace F6M01.Mensageria.Tests.Infra;

/// <summary>
/// PRONTO. Espera uma condição ficar verdadeira com polling curto e TIMEOUT — nunca um
/// <c>Task.Delay</c> fixo "para dar tempo de a mensagem chegar". Se a condição já é verdadeira,
/// retorna na hora; se nunca fica, falha com uma mensagem clara.
/// </summary>
public static class Eventualmente
{
    public static readonly TimeSpan TimeoutPadrao = TimeSpan.FromSeconds(5);

    public static async Task Ate(Func<Task<bool>> condicao, string descricao, TimeSpan? timeout = null)
    {
        var limite = DateTime.UtcNow + (timeout ?? TimeoutPadrao);
        while (true)
        {
            if (await condicao())
                return;
            if (DateTime.UtcNow > limite)
                throw new TimeoutException($"Eventualmente: {descricao} (após {(timeout ?? TimeoutPadrao).TotalSeconds} s)");
            await Task.Delay(20);
        }
    }

    public static Task Ate(Func<bool> condicao, string descricao, TimeSpan? timeout = null) =>
        Ate(() => Task.FromResult(condicao()), descricao, timeout);

    /// <summary>Espera uma tarefa terminar, com timeout (para TaskCompletionSource sinalizado por consumidor).</summary>
    public static async Task<T> Aguardar<T>(Task<T> tarefa, string descricao, TimeSpan? timeout = null)
    {
        try
        {
            return await tarefa.WaitAsync(timeout ?? TimeoutPadrao);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Eventualmente: {descricao} (após {(timeout ?? TimeoutPadrao).TotalSeconds} s)");
        }
    }
}
