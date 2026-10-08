using System.Text.RegularExpressions;
using F4M02.Pedidos.Domain.Comum;

namespace F4M02.Pedidos.Domain.ValueObjects;

/// <summary>
/// SKU (Stock Keeping Unit): código do produto no catálogo. Sem espaços nas pontas, em maiúsculas,
/// de 3 a 20 caracteres, só letras/dígitos em blocos separados por hífen (ex.: <c>CAFE-500G</c>).
/// </summary>
public sealed partial record Sku
{
    public const int TamanhoMinimo = 3;
    public const int TamanhoMaximo = 20;

    /// <exception cref="RegraDeNegocioVioladaException">Se o código não seguir o padrão (<see cref="Regras.SkuInvalido"/>).</exception>
    public Sku(string valor) =>
        throw new NotImplementedException("TODO (Passo 1): normalize (Trim + ToUpperInvariant), valide o tamanho e Padrao(); inválido → Regras.SkuInvalido.");

    public string Valor { get; } = string.Empty;

    public override string ToString() => Valor;

    /// <summary>PRONTO: blocos de letras/dígitos separados por um hífen. Valide DEPOIS de normalizar.</summary>
    [GeneratedRegex("^[A-Z0-9]+(-[A-Z0-9]+)*$")]
    private static partial Regex Padrao();
}
