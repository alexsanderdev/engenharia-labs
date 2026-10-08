using System.Diagnostics;

namespace F6M05.Tests.Infra;

/// <summary>
/// PRONTA. Espera uma condição ficar verdadeira (polling curto com prazo). É assim que se testa
/// algo assíncrono via broker SEM <c>Task.Delay</c> fixo: o teste termina assim que a condição
/// vale e só falha se o prazo estourar.
/// </summary>
public static class Esperar
{
    public static readonly TimeSpan PrazoPadrao = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Intervalo = TimeSpan.FromMilliseconds(25);

    /// <summary>Repete <paramref name="condicao"/> até ser verdadeira; estoura com <paramref name="descricao"/>.</summary>
    public static async Task Eventualmente(Func<Task<bool>> condicao, string descricao, TimeSpan? prazo = null)
    {
        var limite = prazo ?? PrazoPadrao;
        var relogio = Stopwatch.StartNew();
        Exception? ultimoErro = null;
        while (relogio.Elapsed < limite)
        {
            try
            {
                if (await condicao()) return;
            }
            catch (Exception ex)
            {
                ultimoErro = ex;
            }
            await Task.Delay(Intervalo);
        }
        throw new TimeoutException(
            $"Não aconteceu em {limite.TotalSeconds:0.#} s: {descricao}" + (ultimoErro is null ? "" : $" (último erro: {ultimoErro.Message})"),
            ultimoErro);
    }

    /// <summary>Versão síncrona da condição.</summary>
    public static Task Eventualmente(Func<bool> condicao, string descricao, TimeSpan? prazo = null) =>
        Eventualmente(() => Task.FromResult(condicao()), descricao, prazo);
}
