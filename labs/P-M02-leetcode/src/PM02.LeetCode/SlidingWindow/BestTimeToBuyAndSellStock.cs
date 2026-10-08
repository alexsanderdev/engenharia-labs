namespace PM02.LeetCode;

/// <summary>
/// Semana 7 · Padrão: sliding window / varredura única guardando o mínimo · Esperado: O(n) tempo, O(1) memória.
/// <para>
/// Enunciado: <c>precos[i]</c> é o preço de um produto no dia i. Você pode comprar UMA vez e vender
/// UMA vez, num dia posterior à compra. Devolva o maior lucro possível, ou 0 se não houver lucro.
/// </para>
/// Ideia: percorra os dias guardando o menor preço visto até agora; o lucro de vender hoje é
/// preço de hoje − menor preço anterior. Solução ingênua: todos os pares (compra, venda), O(n²).
/// </summary>
public static class BestTimeToBuyAndSellStock
{
    public static int Resolver(int[] precos)
    {
        // TODO: implemente até os testes deste problema passarem. Registre complexidade, padrão e alternativa.
        throw new NotImplementedException("TODO: Best Time to Buy and Sell Stock — uma passada guardando o menor preço até agora e o melhor lucro.");
    }
}
