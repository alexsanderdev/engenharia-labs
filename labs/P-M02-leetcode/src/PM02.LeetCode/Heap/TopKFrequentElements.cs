namespace PM02.LeetCode;

/// <summary>
/// Semana 12 · Padrão: heap (min-heap de tamanho k) + hash map · Esperado: O(n log k) tempo, O(n) memória.
/// <para>
/// Enunciado: dado um array de inteiros e um número k, devolva os k valores que mais aparecem.
/// A ordem da resposta não importa. Considere que a resposta é única (não há empate na fronteira).
/// </para>
/// Alternativas: ordenar por frequência, O(n log n); bucket sort por frequência, O(n).
/// </summary>
public static class TopKFrequentElements
{
    public static int[] Resolver(int[] numeros, int k)
    {
        // TODO: implemente até os testes deste problema passarem. Registre complexidade, padrão e alternativa.
        throw new NotImplementedException("TODO: Top K Frequent — conte frequências e mantenha um PriorityQueue (min-heap) com no máximo k itens.");
    }
}
