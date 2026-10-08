using System.Security.Cryptography;

namespace F5M03.Api.Dominio;

/// <summary>
/// ARQUIVO PRONTO — não precisa alterar.
/// Hash de senha com PBKDF2-HMAC-SHA256 e salt aleatório. Em produção, prefira uma
/// implementação mantida (ex.: <c>PasswordHasher</c> do ASP.NET Core Identity) ou Argon2id;
/// aqui o objetivo é só nunca guardar (nem devolver, nem logar) a senha em texto puro.
/// </summary>
public static class HashDeSenha
{
    // OWASP Password Storage Cheat Sheet recomenda >= 600.000 iterações para PBKDF2-HMAC-SHA256.
    private const int Iteracoes = 600_000;
    private const int TamanhoDoSalt = 16;
    private const int TamanhoDoHash = 32;

    public static string Gerar(string senha)
    {
        ArgumentNullException.ThrowIfNull(senha);
        var salt = RandomNumberGenerator.GetBytes(TamanhoDoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoDoHash);
        return $"PBKDF2-SHA256${Iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string hashArmazenado)
    {
        var partes = hashArmazenado.Split('$');
        if (partes.Length != 4 || !int.TryParse(partes[1], out var iteracoes)) return false;
        var salt = Convert.FromBase64String(partes[2]);
        var esperado = Convert.FromBase64String(partes[3]);
        var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, esperado.Length);
        // Comparação em tempo constante: não vaza, pelo tempo de resposta, quantos bytes bateram.
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
