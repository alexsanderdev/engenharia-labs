namespace PM02.LeetCode;

/// <summary>
/// Semana 6 · Padrão: sliding window (janela variável) + hash map · Esperado: O(n) tempo, O(k) memória.
/// <para>
/// Enunciado: devolva o tamanho do maior trecho CONTÍNUO da string em que nenhum caractere se repete.
/// </para>
/// Ideia: a janela [inicio, fim] nunca tem repetidos. Ao encontrar um caractere já visto DENTRO da janela,
/// o início pula para logo depois da última ocorrência dele. Armadilha: a última ocorrência pode estar
/// ANTES do início atual (caso "abba"), e a janela nunca anda para trás.
/// Solução ingênua: testar todas as substrings, O(n²) a O(n³).
/// </summary>
public static class LongestSubstringWithoutRepeatingCharacters
{
    public static int Resolver(string s)
    {
        var ultimaPosicao = new Dictionary<char, int>();
        int inicio = 0, melhor = 0;

        for (var fim = 0; fim < s.Length; fim++)
        {
            if (ultimaPosicao.TryGetValue(s[fim], out var anterior) && anterior >= inicio)
                inicio = anterior + 1;

            ultimaPosicao[s[fim]] = fim;
            melhor = Math.Max(melhor, fim - inicio + 1);
        }

        return melhor;
    }
}
