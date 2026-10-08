namespace F2M03.CodeSmells.ShotgunSurgery;

/// <summary>Importa linhas "SKU;QUANTIDADE" vindas de um parceiro.</summary>
public sealed class ImportadorDePedidosCsv
{
    private readonly PoliticaDeQuantidade _politica;

    /// <summary>Usa a <see cref="PoliticaDeQuantidade.Padrao"/>.</summary>
    public ImportadorDePedidosCsv() : this(PoliticaDeQuantidade.Padrao) { }

    public ImportadorDePedidosCsv(PoliticaDeQuantidade politica) => _politica = politica;

    /// <summary>
    /// Devolve as linhas válidas. Linhas mal formadas, sem SKU, com quantidade não numérica
    /// ou fora da política são ignoradas.
    /// </summary>
    public IReadOnlyList<(string Sku, int Quantidade)> Importar(IEnumerable<string> linhas)
    {
        ArgumentNullException.ThrowIfNull(linhas);

        var resultado = new List<(string, int)>();
        foreach (var linha in linhas)
        {
            var partes = linha.Split(';');
            if (partes.Length != 2 || string.IsNullOrWhiteSpace(partes[0]))
                continue;

            if (int.TryParse(partes[1].Trim(), out var quantidade) && _politica.Permite(quantidade))
                resultado.Add((partes[0].Trim(), quantidade));
        }

        return resultado;
    }
}
