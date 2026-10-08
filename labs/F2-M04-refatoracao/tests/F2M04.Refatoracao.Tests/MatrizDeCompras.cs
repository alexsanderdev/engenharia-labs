namespace F2M04.Refatoracao.Tests;

/// <summary>
/// Combinações de entrada usadas no teste de aprovação. Os valores escolhidos ficam nas bordas das regras
/// (49,99 / 50; 99,99 / 100; 199,99 / 200) e em valores grandes o bastante para bater nos tetos.
/// </summary>
internal static class MatrizDeCompras
{
    public static readonly string[] Categorias = ["BRONZE", "PRATA", "OURO", " ouro ", "XPTO"];

    public static readonly decimal[] Valores = [0m, 49.99m, 50m, 99.99m, 100m, 199.99m, 200m, 1234.56m, 3000m];

    public static readonly string[] Pagamentos = ["PIX", "CARTAO", "BOLETO", "DINHEIRO"];

    public static readonly bool[] SimNao = [false, true];

    /// <summary>Produto cartesiano de todas as entradas (5 × 9 × 4 × 2 × 2 = 720 combinações).</summary>
    public static IEnumerable<(string Categoria, decimal Valor, string Pagamento, bool PrimeiraCompra, bool Aniversario)> Combinacoes() =>
        from categoria in Categorias
        from valor in Valores
        from pagamento in Pagamentos
        from primeira in SimNao
        from aniversario in SimNao
        select (categoria, valor, pagamento, primeira, aniversario);
}
