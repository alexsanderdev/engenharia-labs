namespace PM02.LeetCode;

/// <summary>
/// Semana 5 · Padrão: two pointers (descartar o lado limitante) · Esperado: O(n) tempo, O(1) memória.
/// <para>
/// Enunciado: cada posição i do array é uma parede vertical de altura <c>alturas[i]</c>.
/// Escolha duas paredes que, junto com o chão, formem o recipiente com a maior área de água:
/// <c>min(altura esquerda, altura direita) × distância entre elas</c>. Devolva essa área.
/// </para>
/// Por que funciona: mover o ponteiro da parede MAIS ALTA nunca melhora a área
/// (a largura diminui e a altura continua limitada pela mais baixa), então sempre movemos a mais baixa.
/// Solução ingênua: testar todos os pares, O(n²).
/// </summary>
public static class ContainerWithMostWater
{
    public static int Resolver(int[] alturas)
    {
        int esquerda = 0, direita = alturas.Length - 1, melhor = 0;

        while (esquerda < direita)
        {
            var altura = Math.Min(alturas[esquerda], alturas[direita]);
            melhor = Math.Max(melhor, altura * (direita - esquerda));

            if (alturas[esquerda] < alturas[direita])
                esquerda++;
            else
                direita--;
        }

        return melhor;
    }
}
