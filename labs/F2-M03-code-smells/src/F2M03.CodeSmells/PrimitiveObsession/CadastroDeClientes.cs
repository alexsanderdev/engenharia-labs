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

        var cpfValido = Cpf.Criar(cpf);
        var emailValido = Email.Criar(email);
        var limite = new Dinheiro(limiteDeCredito);

        return new ClienteLegado(nome.Trim(), cpfValido.Numero, emailValido.Endereco, limite.Valor);
    }

    /// <summary>Formata um CPF (com ou sem máscara) para exibição: 000.000.000-00.</summary>
    public static string FormatarCpf(string cpf) => Cpf.Criar(cpf).Formatado;
}
