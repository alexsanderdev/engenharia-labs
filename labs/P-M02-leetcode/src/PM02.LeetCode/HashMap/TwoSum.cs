namespace PM02.LeetCode;

/// <summary>
/// Semana 1 · Padrão: hash map · Esperado: O(n) tempo, O(n) memória.
/// <para>
/// Enunciado: dado um array de inteiros e um valor alvo, devolva os índices [i, j] (i &lt; j)
/// de dois elementos DIFERENTES cuja soma é o alvo. Se não houver par, devolva um array vazio.
/// Se houver mais de um par, devolva aquele cujo segundo índice (j) aparece primeiro.
/// </para>
/// Solução ingênua: dois loops aninhados, O(n²).
/// </summary>
public static class TwoSum
{
    public static int[] Resolver(int[] numeros, int alvo)
    {
        // valor já visto -> índice onde ele apareceu
        var vistos = new Dictionary<int, int>(numeros.Length);

        for (var j = 0; j < numeros.Length; j++)
        {
            var complemento = alvo - numeros[j];
            if (vistos.TryGetValue(complemento, out var i))
                return [i, j];

            // Só registra DEPOIS de procurar: assim o elemento não casa consigo mesmo.
            vistos.TryAdd(numeros[j], j);
        }

        return [];
    }
}
