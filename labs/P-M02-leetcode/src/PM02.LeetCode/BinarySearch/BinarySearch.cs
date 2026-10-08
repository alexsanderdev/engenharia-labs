namespace PM02.LeetCode;

/// <summary>
/// Semana 10 · Padrão: binary search · Esperado: O(log n) tempo, O(1) memória.
/// <para>
/// Enunciado: dado um array de inteiros ORDENADO de forma crescente e sem repetidos,
/// devolva o índice do alvo ou -1 se ele não existir. Não use Array.BinarySearch.
/// </para>
/// Armadilhas: condição do laço (&lt;= vs &lt;), atualizar com meio ± 1 e calcular o meio
/// sem estourar int (<c>esquerda + (direita - esquerda) / 2</c>).
/// </summary>
public static class BinarySearch
{
    public static int Resolver(int[] numeros, int alvo)
    {
        // TODO: implemente até os testes deste problema passarem. Registre complexidade, padrão e alternativa.
        throw new NotImplementedException("TODO: Binary Search — esquerda/direita, meio = esquerda + (direita - esquerda) / 2, laço enquanto esquerda <= direita.");
    }
}
