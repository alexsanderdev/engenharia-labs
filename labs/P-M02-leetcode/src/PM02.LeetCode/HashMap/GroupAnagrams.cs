namespace PM02.LeetCode;

/// <summary>
/// Semana 3 · Padrão: hash map com chave canônica · Esperado: O(n · k log k) tempo, O(n · k) memória
/// (n palavras de tamanho até k).
/// <para>
/// Enunciado: dada uma lista de palavras, agrupe as que são anagramas entre si.
/// A ordem dos grupos e a ordem dentro de cada grupo não importam.
/// </para>
/// Ideia: anagramas têm a mesma "assinatura" (letras ordenadas); use a assinatura como chave.
/// Alternativa O(n · k): assinatura por contagem de letras (só vale para alfabeto pequeno e conhecido).
/// </summary>
public static class GroupAnagrams
{
    public static IList<IList<string>> Resolver(string[] palavras)
    {
        // TODO: implemente até os testes deste problema passarem. Registre complexidade, padrão e alternativa.
        throw new NotImplementedException("TODO: Group Anagrams — gere uma assinatura canônica por palavra (letras ordenadas) e agrupe num Dictionary<string, IList<string>>.");
    }
}
