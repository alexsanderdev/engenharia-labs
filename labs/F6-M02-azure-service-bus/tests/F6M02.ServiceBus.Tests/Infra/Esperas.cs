using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using F6M02.ServiceBus.Consumo;

namespace F6M02.ServiceBus.Tests.Infra;

/// <summary>
/// Esperas para testes assíncronos com broker: NUNCA "Task.Delay(2000) e torcer".
/// Ou o teste espera um sinal (TaskCompletionSource), ou faz polling com timeout curto, ou faz uma
/// leitura com timeout (o próprio ReceiveMessagesAsync espera até chegar algo).
/// </summary>
public static class Esperas
{
    /// <summary>Timeout padrão: folgado para máquina lenta, curto o bastante para a suíte não travar.</summary>
    public static readonly TimeSpan Padrao = TimeSpan.FromSeconds(15);

    /// <summary>Repete a verificação até ela passar ou o tempo acabar (polling a cada 100 ms).</summary>
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

    /// <summary>Espera um sinal sem valor.</summary>
    public static Task Sinal(TaskCompletionSource sinal, string descricao, TimeSpan? timeout = null) =>
        Esperar(sinal.Task, descricao, timeout);

    /// <summary>Espera um sinal com timeout, com mensagem clara se não vier.</summary>
    public static async Task<T> Sinal<T>(TaskCompletionSource<T> sinal, string descricao, TimeSpan? timeout = null)
    {
        await Esperar(sinal.Task, descricao, timeout);
        return await sinal.Task;
    }

    private static async Task Esperar(Task tarefa, string descricao, TimeSpan? timeout)
    {
        try
        {
            await tarefa.WaitAsync(timeout ?? Padrao);
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"Sinal não chegou em {(timeout ?? Padrao).TotalSeconds:0} s: {descricao}");
        }
    }

    /// <summary>
    /// Recebe (peek-lock + complete) da <paramref name="origem"/> até <paramref name="chegouTudo"/> ser verdadeiro
    /// ou o tempo acabar, e devolve tudo o que recebeu (o teste decide se está certo).
    /// </summary>
    public static async Task<List<ServiceBusReceivedMessage>> ReceberAte(
        ServiceBusClient cliente,
        OrigemDasMensagens origem,
        Func<IReadOnlyList<ServiceBusReceivedMessage>, bool> chegouTudo,
        TimeSpan? timeout = null)
    {
        var limite = timeout ?? Padrao;
        var recebidas = new List<ServiceBusReceivedMessage>();
        var relogio = Stopwatch.StartNew();
        await using var receiver = origem.CriarReceiver(cliente);
        while (!chegouTudo(recebidas) && relogio.Elapsed < limite)
        {
            var lote = await receiver.ReceiveMessagesAsync(50, TimeSpan.FromMilliseconds(500));
            foreach (var m in lote)
            {
                recebidas.Add(m);
                await receiver.CompleteMessageAsync(m);
            }
        }
        return recebidas;
    }
}
