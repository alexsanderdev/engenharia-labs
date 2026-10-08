namespace F2M04.Refatoracao.Tests;

/// <summary>
/// Testes de DESIGN: a API nova (<see cref="ProgramaDeFidelidade"/>, <see cref="RegrasDaCategoria"/>) e a regra nova
/// (categoria Diamante). Começam vermelhos.
/// </summary>
public class DesignProgramaDeFidelidadeTests
{
    [Theory]
    [InlineData(Categoria.Bronze, 1, 0.10, 0, 200, 1000)]
    [InlineData(Categoria.Prata, 1.5, 0.10, 0, 100, 3000)]
    [InlineData(Categoria.Ouro, 2, 0.10, 0.05, 0, 5000)]
    public void RegrasDaCategoria_De_TabelaComAsRegrasDoLegado(
        Categoria categoria, decimal multiplicador, decimal bonusPix, decimal bonusCartao, decimal minimoAniversario, int teto)
    {
        RegrasDaCategoria.De(categoria).ShouldBe(new RegrasDaCategoria(multiplicador, bonusPix, bonusCartao, minimoAniversario, teto));
    }

    [Fact]
    public void CalcularPontos_TodaAMatriz_MesmoResultadoDoLegado()
    {
        var legado = new CalculadoraDePontos();
        var programa = new ProgramaDeFidelidade();
        var diferencas = new List<string>();

        foreach (var (categoria, valor, pagamento, primeira, aniversario) in MatrizDeCompras.Combinacoes())
        {
            var compra = new Compra(ParaCategoria(categoria), valor, ParaPagamento(pagamento), primeira, aniversario);

            var esperado = legado.Calcular(categoria, valor, pagamento, primeira, aniversario);
            var obtido = programa.CalcularPontos(compra);

            if (obtido != esperado)
                diferencas.Add($"{compra} => esperado {esperado}, obtido {obtido}");
        }

        diferencas.ShouldBeEmpty();
    }

    [Fact]
    public void CalcularPontos_ValorNegativo_LancaArgumentOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            new ProgramaDeFidelidade().CalcularPontos(new Compra(Categoria.Ouro, -1m, FormaDePagamento.Pix, false, false)));
    }

    // ---------- Regra nova: categoria Diamante ----------

    [Fact]
    public void RegrasDaCategoria_Diamante_TriploDePontosCartaoComBonusAniversarioSemMinimoETetoDe10Mil()
    {
        RegrasDaCategoria.De(Categoria.Diamante).ShouldBe(new RegrasDaCategoria(
            Multiplicador: 3m, BonusPix: 0.10m, BonusCartao: 0.10m, ValorMinimoAniversario: 0m, Teto: 10_000));
    }

    [Theory]
    [InlineData(100, FormaDePagamento.Pix, false, false, 330)]
    [InlineData(1000, FormaDePagamento.Boleto, false, false, 3000)]
    [InlineData(100, FormaDePagamento.Cartao, true, true, 760)]
    [InlineData(50, FormaDePagamento.Outra, false, true, 300)]
    [InlineData(49.99, FormaDePagamento.Pix, true, true, 10)]
    [InlineData(2000, FormaDePagamento.Pix, false, true, 10_000)]
    public void CalcularPontos_Diamante(decimal valor, FormaDePagamento pagamento, bool primeira, bool aniversario, int esperado)
    {
        new ProgramaDeFidelidade()
            .CalcularPontos(new Compra(Categoria.Diamante, valor, pagamento, primeira, aniversario))
            .ShouldBe(esperado);
    }

    private static Categoria ParaCategoria(string texto) => texto.Trim().ToUpperInvariant() switch
    {
        "OURO" => Categoria.Ouro,
        "PRATA" => Categoria.Prata,
        _ => Categoria.Bronze,
    };

    private static FormaDePagamento ParaPagamento(string texto) => texto switch
    {
        "PIX" => FormaDePagamento.Pix,
        "CARTAO" => FormaDePagamento.Cartao,
        "BOLETO" => FormaDePagamento.Boleto,
        _ => FormaDePagamento.Outra,
    };
}
