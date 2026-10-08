namespace PM02.LeetCode;

/// <summary>
/// Semana 8 · Padrão: stack · Esperado: O(n) tempo, O(n) memória.
/// <para>
/// Enunciado: a string contém apenas os caracteres ( ) [ ] { }. Diga se ela é válida:
/// todo símbolo aberto é fechado pelo mesmo tipo e na ordem correta (o último aberto é o primeiro fechado).
/// String vazia é válida.
/// </para>
/// Armadilhas: fechar com a pilha vazia e terminar com aberturas sobrando na pilha.
/// </summary>
public static class ValidParentheses
{
    public static bool Resolver(string s)
    {
        var abertos = new Stack<char>();

        foreach (var c in s)
        {
            if (c is '(' or '[' or '{')
            {
                abertos.Push(c);
                continue;
            }

            var esperado = c switch
            {
                ')' => '(',
                ']' => '[',
                '}' => '{',
                _ => throw new ArgumentException($"Caractere inválido: '{c}'.", nameof(s)),
            };

            if (!abertos.TryPop(out var topo) || topo != esperado)
                return false;
        }

        return abertos.Count == 0;
    }
}
