namespace PM02.LeetCode;

/// <summary>
/// Semana 11 · Padrão: binary search modificada · Esperado: O(log n) tempo, O(1) memória.
/// <para>
/// Enunciado: um array crescente e sem repetidos foi "girado" num ponto desconhecido
/// (ex.: [0,1,2,4,5,6,7] virou [4,5,6,7,0,1,2]). Devolva o índice do alvo ou -1, em O(log n).
/// </para>
/// Ideia: em qualquer corte [esquerda, meio, direita], pelo menos UMA das metades está ordenada.
/// Descubra qual, verifique se o alvo cai no intervalo dela e descarte a outra metade.
/// Solução ingênua: busca linear, O(n).
/// </summary>
public static class SearchInRotatedSortedArray
{
    public static int Resolver(int[] numeros, int alvo)
    {
        // TODO: implemente até os testes deste problema passarem. Registre complexidade, padrão e alternativa.
        throw new NotImplementedException("TODO: Search in Rotated Sorted Array — a cada passo descubra qual metade está ordenada e se o alvo cai nela.");
    }
}
