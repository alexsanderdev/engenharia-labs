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
    public Sku(string valor)
    {
        var normalizado = valor?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizado.Length is < TamanhoMinimo or > TamanhoMaximo || !Padrao().IsMatch(normalizado))
            throw new RegraDeNegocioVioladaException(Regras.SkuInvalido, $"SKU '{valor}' inválido: use {TamanhoMinimo} a {TamanhoMaximo} letras/dígitos em blocos separados por hífen.");
        Valor = normalizado;
    }

    public string Valor { get; }

    public override string ToString() => Valor;

    [GeneratedRegex("^[A-Z0-9]+(-[A-Z0-9]+)*$")]
    private static partial Regex Padrao();
}
