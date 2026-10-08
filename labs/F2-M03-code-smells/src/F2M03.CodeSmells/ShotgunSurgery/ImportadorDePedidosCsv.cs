namespace F2M03.CodeSmells.ShotgunSurgery;

/// <summary>Importa linhas "SKU;QUANTIDADE" vindas de um parceiro.</summary>
public sealed class ImportadorDePedidosCsv
{
    private const int QuantidadeMaxima = 10; // e de novo, agora com outro nome

    public ImportadorDePedidosCsv() { }

    /// <summary>Importador com uma política de quantidade explícita.</summary>
    public ImportadorDePedidosCsv(PoliticaDeQuantidade politica) =>
        throw new NotImplementedException("TODO: guarde a política e faça o construtor sem parâmetros usar PoliticaDeQuantidade.Padrao.");

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

            if (int.TryParse(partes[1].Trim(), out var quantidade) && quantidade >= 1 && quantidade <= QuantidadeMaxima)
                resultado.Add((partes[0].Trim(), quantidade));
        }

        return resultado;
    }
}
