namespace F2M03.CodeSmells.PrimitiveObsession;

/// <summary>
/// Cliente como o legado devolve: tudo primitivo (CPF e e-mail como string, limite como decimal).
/// O contrato é mantido para não quebrar quem já consome a classe.
/// </summary>
public sealed record ClienteLegado(string Nome, string Cpf, string Email, decimal LimiteDeCredito);

/// <summary>
/// SMELL: Primitive Obsession. CPF, e-mail e dinheiro circulam como <c>string</c>/<c>decimal</c> soltos,
/// e as regras de cada conceito (dígitos verificadores, normalização, arredondamento) ficam espalhadas aqui.
/// Depois da refatoração este método vira uma fachada fina que delega para <see cref="Cpf"/>,
/// <see cref="Email"/> e <see cref="Dinheiro"/>.
/// </summary>
public sealed class CadastroDeClientes
{
    /// <summary>Valida e normaliza os dados de um cliente novo.</summary>
    /// <exception cref="ArgumentException">Nome vazio, CPF inválido, e-mail inválido ou limite negativo.</exception>
    public ClienteLegado Cadastrar(string nome, string cpf, string email, decimal limiteDeCredito)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        // CPF: tira a máscara e confere os dígitos verificadores
        var digitos = new string((cpf ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        if (digitos.Length != 11 || digitos.Distinct().Count() == 1)
            throw new ArgumentException("CPF inválido.", nameof(cpf));

        var soma = 0;
        for (var i = 0; i < 9; i++)
            soma += (digitos[i] - '0') * (10 - i);
        var dv1 = soma % 11 < 2 ? 0 : 11 - soma % 11;

        soma = 0;
        for (var i = 0; i < 10; i++)
            soma += (digitos[i] - '0') * (11 - i);
        var dv2 = soma % 11 < 2 ? 0 : 11 - soma % 11;

        if (digitos[9] - '0' != dv1 || digitos[10] - '0' != dv2)
            throw new ArgumentException("CPF inválido.", nameof(cpf));

        // E-mail: normaliza e confere o formato mínimo
        var emailNormalizado = (email ?? string.Empty).Trim().ToLowerInvariant();
        var partes = emailNormalizado.Split('@');
        if (partes.Length != 2 || partes[0].Length == 0 || !partes[1].Contains('.')
            || partes[1].StartsWith('.') || partes[1].EndsWith('.') || emailNormalizado.Contains(' '))
            throw new ArgumentException("E-mail inválido.", nameof(email));

        // Dinheiro: não negativo, 2 casas
        if (limiteDeCredito < 0)
            throw new ArgumentException("Valor monetário não pode ser negativo.", nameof(limiteDeCredito));
        var limite = Math.Round(limiteDeCredito, 2, MidpointRounding.AwayFromZero);

        return new ClienteLegado(nome.Trim(), digitos, emailNormalizado, limite);
    }

    /// <summary>Formata um CPF (com ou sem máscara) para exibição: 000.000.000-00.</summary>
    public static string FormatarCpf(string cpf)
    {
        // A mesma limpeza de máscara, repetida (e sem validar nada!)
        var d = new string((cpf ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
        return $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}";
    }
}
