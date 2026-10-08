namespace PM02.LeetCode;

/// <summary>
/// Semana 2 · Padrão: hash map (contagem de frequência) · Esperado: O(n) tempo, O(k) memória (k = alfabeto).
/// <para>
/// Enunciado: diga se a string <c>t</c> é um anagrama de <c>s</c>, isto é, se usa exatamente
/// as mesmas letras com as mesmas quantidades, em qualquer ordem. Considere qualquer caractere
/// (inclusive acentuados) e diferencie maiúsculas de minúsculas.
/// </para>
/// Alternativa: ordenar as duas strings e comparar, O(n log n).
/// </summary>
public static class ValidAnagram
{
    public static bool Resolver(string s, string t)
    {
        if (s.Length != t.Length)
            return false;

        var contagem = new Dictionary<char, int>();
        foreach (var c in s)
            contagem[c] = contagem.GetValueOrDefault(c) + 1;

        foreach (var c in t)
        {
            var restante = contagem.GetValueOrDefault(c) - 1;
            if (restante < 0)
                return false;

            contagem[c] = restante;
        }

        return true;
    }
}
