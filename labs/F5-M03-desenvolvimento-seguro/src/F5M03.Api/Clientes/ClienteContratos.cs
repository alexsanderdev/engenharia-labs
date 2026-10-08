using System.Net.Mail;
using F5M03.Api.Seguranca;
using F5M03.Api.Validacao;

namespace F5M03.Api.Clientes;

/// <summary>
/// Contrato de ENTRADA do cadastro público: só o que o cliente pode escolher.
/// Não existe IsAdmin aqui — o que não está no contrato não pode ser atribuído (mass assignment).
/// </summary>
public sealed record CadastrarClienteRequest(string? Nome, string? Email, string? Cpf, string? Senha)
{
    // Records geram ToString() com TODAS as propriedades. Um logger.LogInformation("{Req}", req)
    // esquecido em qualquer lugar vazaria a senha. Defesa em profundidade: ToString seguro.
    public override string ToString() =>
        $"CadastrarClienteRequest {{ Nome = {Nome}, Email = {Mascaramento.Email(Email)}, Cpf = {Mascaramento.Cpf(Cpf)}, Senha = *** }}";
}

/// <summary>Contrato de SAÍDA: sem SenhaHash, sem IsAdmin, CPF mascarado.</summary>
public sealed record ClienteResponse(Guid Id, string Nome, string Email, string CpfMascarado);

public static class ValidadorDeCliente
{
    public const int NomeMinimo = 2;
    public const int NomeMaximo = 100;
    public const int EmailMaximo = 254;
    public const int SenhaMinima = 12;
    public const int SenhaMaxima = 128;

    public static ErrosDeValidacao Validar(CadastrarClienteRequest r)
    {
        var erros = new ErrosDeValidacao();
        var nome = r.Nome?.Trim() ?? "";

        erros.Se(nome.Length is < NomeMinimo or > NomeMaximo, nameof(r.Nome),
            $"Nome deve ter entre {NomeMinimo} e {NomeMaximo} caracteres.");
        erros.Se(!EmailValido(r.Email), nameof(r.Email), "E-mail inválido.");
        erros.Se(!CpfValido(r.Cpf), nameof(r.Cpf), "CPF inválido.");
        erros.Se((r.Senha?.Length ?? 0) is < SenhaMinima or > SenhaMaxima, nameof(r.Senha),
            $"Senha deve ter entre {SenhaMinima} e {SenhaMaxima} caracteres.");

        return erros;
    }

    public static bool EmailValido(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.Length <= EmailMaximo
        && MailAddress.TryCreate(email, out var endereco)
        && endereco.Address == email // rejeita "Nome <a@b.com>" e espaços extras
        && endereco.Host.Contains('.');

    public static string SomenteDigitos(string? valor) =>
        new((valor ?? "").Where(char.IsAsciiDigit).ToArray());

    /// <summary>Aceita "52998224725" ou "529.982.247-25"; confere os dígitos verificadores.</summary>
    public static bool CpfValido(string? cpf)
    {
        if (cpf is null) return false;
        // Formato: só dígitos, ou com a máscara padrão. Qualquer outro caractere reprova.
        if (cpf.Any(c => !char.IsAsciiDigit(c) && c is not '.' and not '-')) return false;

        var d = SomenteDigitos(cpf);
        if (d.Length != 11 || d.Distinct().Count() == 1) return false;

        static int Digito(string digitos, int quantidade)
        {
            var soma = 0;
            for (var i = 0; i < quantidade; i++)
                soma += (digitos[i] - '0') * (quantidade + 1 - i);
            var resto = soma % 11;
            return resto < 2 ? 0 : 11 - resto;
        }

        return Digito(d, 9) == d[9] - '0' && Digito(d, 10) == d[10] - '0';
    }
}
