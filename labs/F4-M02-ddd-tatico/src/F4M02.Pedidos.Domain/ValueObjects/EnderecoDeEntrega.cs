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
    public EnderecoDeEntrega(string logradouro, string numero, string cidade, string uf, string cep) =>
        throw new NotImplementedException("TODO (Passo 1): valide e normalize cada campo (Trim; UF maiúscula com 2 letras; CEP com 8 dígitos, sem '-' e '.') lançando Regras.EnderecoInvalido.");

    public string Logradouro { get; } = string.Empty;
    public string Numero { get; } = string.Empty;
    public string Cidade { get; } = string.Empty;
    public string Uf { get; } = string.Empty;

    /// <summary>Somente os 8 dígitos.</summary>
    public string Cep { get; } = string.Empty;
}
