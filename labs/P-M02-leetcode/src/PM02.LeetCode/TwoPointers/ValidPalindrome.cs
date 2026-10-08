namespace PM02.LeetCode;

/// <summary>
/// Semana 4 · Padrão: two pointers (extremidades convergindo) · Esperado: O(n) tempo, O(1) memória.
/// <para>
/// Enunciado: diga se a frase é um palíndromo considerando apenas letras e dígitos
/// e ignorando maiúsculas/minúsculas. Acentos NÃO são removidos ('ô' é diferente de 'o').
/// String vazia ou só com símbolos é palíndromo.
/// </para>
/// Alternativa: filtrar para uma nova string e comparar com o reverso, O(n) de memória extra.
/// </summary>
public static class ValidPalindrome
{
    public static bool Resolver(string s)
    {
        int esquerda = 0, direita = s.Length - 1;

        while (esquerda < direita)
        {
            if (!char.IsLetterOrDigit(s[esquerda]))
            {
                esquerda++;
                continue;
            }

            if (!char.IsLetterOrDigit(s[direita]))
            {
                direita--;
                continue;
            }

            if (char.ToLowerInvariant(s[esquerda]) != char.ToLowerInvariant(s[direita]))
                return false;

            esquerda++;
            direita--;
        }

        return true;
    }
}
