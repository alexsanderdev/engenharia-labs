using System.Buffers;

namespace F1M05.Memoria.Tests;

/// <summary>Mede bytes alocados no heap pela thread atual.</summary>
internal static class Alocacao
{
    /// <summary>
    /// Executa a ação algumas vezes para aquecer (JIT, caches estáticos, cultura) e depois mede
    /// quantos bytes foram alocados em <paramref name="repeticoes"/> execuções.
    /// A ação (lambda) é criada ANTES da medição, então a closure não entra na conta.
    /// </summary>
    public static long Medir(Action acao, int repeticoes = 100)
    {
        for (var i = 0; i < 10; i++)
            acao();

        var antes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < repeticoes; i++)
            acao();
        return GC.GetAllocatedBytesForCurrentThread() - antes;
    }
}

/// <summary>
/// ArrayPool falso que conta aluguéis e devoluções (e detecta devolução em dobro).
/// </summary>
internal sealed class PoolContador : ArrayPool<char>
{
    private readonly HashSet<char[]> _emprestados = new(ReferenceEqualityComparer.Instance);

    public int Alugueis { get; private set; }
    public int Devolucoes { get; private set; }
    public int Pendentes => _emprestados.Count;

    public override char[] Rent(int minimumLength)
    {
        Alugueis++;
        var array = new char[minimumLength];
        _emprestados.Add(array);
        return array;
    }

    public override void Return(char[] array, bool clearArray = false)
    {
        if (!_emprestados.Remove(array))
            throw new InvalidOperationException("Array devolvido duas vezes ou que não veio deste pool.");
        Devolucoes++;
    }
}
