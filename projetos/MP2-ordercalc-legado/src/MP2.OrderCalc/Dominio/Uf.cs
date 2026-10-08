namespace MP2.OrderCalc.Dominio;

/// <summary>
/// UF de entrega. Comportamento herdado: aceita QUALQUER texto de 2 caracteres
/// (ex.: "XX" cai na faixa "demais estados"). Endurecer isso é mudança de regra, não refatoração.
/// </summary>
public readonly record struct Uf
{
    private Uf(string sigla) => Sigla = sigla;

    public string Sigla { get; }

    public bool EhSudeste => Sigla is "SP" or "RJ" or "MG" or "ES";

    public bool EhSul => Sigla is "PR" or "SC" or "RS";

    public static Uf Ler(string? texto) =>
        texto is { Length: 2 }
            ? new Uf(texto.ToUpperInvariant())
            : throw new PedidoInvalidoException("UF inválida");

    public override string ToString() => Sigla;
}
