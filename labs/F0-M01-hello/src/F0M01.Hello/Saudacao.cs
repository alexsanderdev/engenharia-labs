namespace F0M01.Hello;

/// <summary>
/// Primeiro lab: valida que seu ambiente (SDK, IDE, testes, CI) está funcionando.
/// </summary>
public static class Saudacao
{
    /// <summary>
    /// Retorna "Olá, {nome}!". Se o nome for nulo ou vazio, retorna "Olá, mundo!".
    /// O nome deve ser usado sem espaços nas pontas.
    /// </summary>
    public static string Para(string? nome) =>
        string.IsNullOrWhiteSpace(nome) ? "Olá, mundo!" : $"Olá, {nome.Trim()}!";
}
