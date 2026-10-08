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
        var grupos = new Dictionary<string, IList<string>>();

        foreach (var palavra in palavras)
        {
            var letras = palavra.ToCharArray();
            Array.Sort(letras);
            var assinatura = new string(letras);

            if (!grupos.TryGetValue(assinatura, out var grupo))
            {
                grupo = new List<string>();
                grupos[assinatura] = grupo;
            }

            grupo.Add(palavra);
        }

        return [.. grupos.Values];
    }
}
