using F4M02.Pedidos.Domain.Comum;

namespace F4M02.Pedidos.Domain.ValueObjects;

/// <summary>
/// Endereço para onde o pedido vai. Value object composto: não tem identidade — dois endereços com
/// os mesmos dados são o mesmo endereço. Para "mudar" o endereço, troca-se o objeto inteiro.
/// </summary>
/// <remarks>
/// Regras (todas com <see cref="Regras.EnderecoInvalido"/>): logradouro, número e cidade obrigatórios
/// (sem espaços nas pontas); UF com 2 letras, em maiúsculas; CEP com 8 dígitos — aceita
/// <c>01310-100</c> e guarda <c>01310100</c>.
/// </remarks>
public sealed record EnderecoDeEntrega
{
    /// <exception cref="RegraDeNegocioVioladaException">Se algum campo for inválido.</exception>
    public EnderecoDeEntrega(string logradouro, string numero, string cidade, string uf, string cep)
    {
        Logradouro = Obrigatorio(logradouro, "Logradouro");
        Numero = Obrigatorio(numero, "Número");
        Cidade = Obrigatorio(cidade, "Cidade");

        var ufNormalizada = Obrigatorio(uf, "UF").ToUpperInvariant();
        if (ufNormalizada.Length != 2 || !ufNormalizada.All(char.IsAsciiLetterUpper))
            throw Invalido($"UF '{uf}' inválida: use a sigla de 2 letras.");
        Uf = ufNormalizada;

        var cepSoDigitos = (cep ?? string.Empty).Replace("-", string.Empty, StringComparison.Ordinal).Replace(".", string.Empty, StringComparison.Ordinal).Trim();
        if (cepSoDigitos.Length != 8 || !cepSoDigitos.All(char.IsAsciiDigit))
            throw Invalido($"CEP '{cep}' inválido: são 8 dígitos.");
        Cep = cepSoDigitos;
    }

    public string Logradouro { get; }
    public string Numero { get; }
    public string Cidade { get; }
    public string Uf { get; }

    /// <summary>Somente os 8 dígitos.</summary>
    public string Cep { get; }

    public override string ToString() => $"{Logradouro}, {Numero} — {Cidade}/{Uf} — CEP {Cep[..5]}-{Cep[5..]}";

    private static string Obrigatorio(string? valor, string campo) =>
        string.IsNullOrWhiteSpace(valor) ? throw Invalido($"{campo} é obrigatório.") : valor.Trim();

    private static RegraDeNegocioVioladaException Invalido(string mensagem) => new(Regras.EnderecoInvalido, mensagem);
}
