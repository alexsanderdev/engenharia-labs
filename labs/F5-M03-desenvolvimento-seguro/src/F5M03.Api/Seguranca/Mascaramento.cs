namespace F5M03.Api.Seguranca;

/// <summary>
/// Mascaramento de dados pessoais para respostas e logs (LGPD: minimização).
/// Regra geral: mostrar só o suficiente para o humano reconhecer o dado, nunca o bastante para reutilizá-lo.
/// </summary>
public static class Mascaramento
{
    /// <summary>
    /// CPF mascarado mantendo só os 2 dígitos verificadores: "529.982.247-25" ou "52998224725" → "***.***.***-25".
    /// Entrada nula, vazia ou sem 11 dígitos → "***".
    /// </summary>
    public static string Cpf(string? cpf) =>
        throw new NotImplementedException("TODO: extraia os dígitos; com 11 dígitos devolva \"***.***.***-\" + os 2 últimos; senão \"***\".");

    /// <summary>
    /// E-mail mascarado mantendo a primeira letra e o domínio: "ana.souza@exemplo.com" → "a***@exemplo.com".
    /// Entrada sem "@" (ou com "@" na primeira/última posição) → "***".
    /// </summary>
    public static string Email(string? email) =>
        throw new NotImplementedException("TODO: primeira letra + \"***\" + a partir do \"@\"; entradas inválidas → \"***\".");
}
