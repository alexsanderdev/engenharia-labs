using F4M02.Pedidos.Domain.Comum;

namespace F4M02.Pedidos.Domain.ValueObjects;

/// <summary>
/// Quantidade de um item: inteiro entre 1 e <see cref="Maxima"/>. Se existe uma <see cref="Quantidade"/>,
/// ela é válida — ninguém mais precisa checar "quantidade &gt; 0".
/// </summary>
/// <remarks>
/// É um <c>record</c> (classe), não um <c>record struct</c>, de propósito: <c>default(QuantidadeStruct)</c>
/// criaria uma quantidade 0 sem passar pelo construtor e furaria a invariante.
/// </remarks>
public sealed record Quantidade
{
    public const int Maxima = 999;

    /// <exception cref="RegraDeNegocioVioladaException">Se <paramref name="valor"/> estiver fora de 1..<see cref="Maxima"/> (<see cref="Regras.QuantidadeInvalida"/>).</exception>
    public Quantidade(int valor) =>
        throw new NotImplementedException("TODO (Passo 1): rejeite valor <= 0 ou > Maxima com Regras.QuantidadeInvalida; guarde em Valor.");

    public int Valor { get; }

    public override string ToString() => Valor.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
