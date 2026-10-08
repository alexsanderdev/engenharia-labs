namespace PM02.LeetCode;

/// <summary>
/// Semana 9 · Padrão: monotonic stack (pilha decrescente de índices) · Esperado: O(n) tempo, O(n) memória.
/// <para>
/// Enunciado: dada a temperatura de cada dia, devolva para cada dia quantos dias é preciso esperar
/// até uma temperatura ESTRITAMENTE maior. Se isso nunca acontecer, o valor é 0.
/// </para>
/// Ideia: a pilha guarda índices de dias ainda "sem resposta", com temperaturas decrescentes.
/// Quando chega um dia mais quente, ele responde todos os dias mais frios do topo.
/// Cada índice entra e sai da pilha uma vez só, por isso é O(n). Solução ingênua: O(n²).
/// </summary>
public static class DailyTemperatures
{
    public static int[] Resolver(int[] temperaturas)
    {
        // TODO: implemente até os testes deste problema passarem. Registre complexidade, padrão e alternativa.
        throw new NotImplementedException("TODO: Daily Temperatures — pilha de índices com temperaturas decrescentes; um dia mais quente responde os do topo.");
    }
}
