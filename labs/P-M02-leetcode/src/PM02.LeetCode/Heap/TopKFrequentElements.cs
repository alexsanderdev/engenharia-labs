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
        var frequencia = new Dictionary<int, int>();
        foreach (var n in numeros)
            frequencia[n] = frequencia.GetValueOrDefault(n) + 1;

        // Min-heap por frequência: o topo é o "menos frequente entre os k melhores".
        var heap = new PriorityQueue<int, int>(k + 1);
        foreach (var (valor, vezes) in frequencia)
        {
            heap.Enqueue(valor, vezes);
            if (heap.Count > k)
                heap.Dequeue(); // descarta o menos frequente: o heap nunca passa de k itens
        }

        var resposta = new int[heap.Count];
        for (var i = 0; heap.Count > 0; i++)
            resposta[i] = heap.Dequeue();

        return resposta;
    }
}
