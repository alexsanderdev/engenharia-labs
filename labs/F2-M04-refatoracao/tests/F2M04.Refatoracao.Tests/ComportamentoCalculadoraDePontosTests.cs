using System.Globalization;
using System.Text;

namespace F2M04.Refatoracao.Tests;

/// <summary>
/// Testes de CARACTERIZAÇÃO do legado. Passam desde o início e NUNCA podem ficar vermelhos durante a refatoração.
/// O principal é um teste de aprovação (approval/snapshot test): gera a saída de 720 combinações e compara com
/// o arquivo <c>ComportamentoCalculadoraDePontosTests.Calcular_MatrizDeCombinacoes_IgualAoAprovado.verified.txt</c>,
/// versionado ao lado deste arquivo.
/// </summary>
public class ComportamentoCalculadoraDePontosTests
{
    [Fact]
    public void Calcular_MatrizDeCombinacoes_IgualAoAprovado()
    {
        var calculadora = new CalculadoraDePontos();
        var saida = new StringBuilder();

        foreach (var (categoria, valor, pagamento, primeira, aniversario) in MatrizDeCompras.Combinacoes())
        {
            var pontos = calculadora.Calcular(categoria, valor, pagamento, primeira, aniversario);

            saida.Append(CultureInfo.InvariantCulture,
                $"{$"[{categoria}]",-10} | {valor,8:0.00} | {pagamento,-8} | primeira={(primeira ? 'S' : 'N')} | aniversario={(aniversario ? 'S' : 'N')} => {pontos}");
            saida.Append('\n');
        }

        Aprovacao.Verificar(saida.ToString());
    }

    [Theory]
    [InlineData("BRONZE", 120, "BOLETO", false, false, 120)]
    [InlineData("OURO", 200, "PIX", false, false, 440)]
    [InlineData("OURO", 200, "CARTAO", true, true, 940)]
    [InlineData("PRATA", 99.99, "PIX", false, true, 163)]
    [InlineData("BRONZE", 30, "PIX", true, false, 10)]
    [InlineData("BRONZE", 30, "BOLETO", true, false, 0)]
    public void Calcular_ExemplosDocumentados(string categoria, decimal valor, string pagamento, bool primeira, bool aniversario, int esperado)
    {
        new CalculadoraDePontos().Calcular(categoria, valor, pagamento, primeira, aniversario).ShouldBe(esperado);
    }

    [Fact]
    public void Calcular_ValorNegativo_LancaArgumentOutOfRange()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new CalculadoraDePontos().Calcular("OURO", -0.01m, "PIX", false, false));
    }
}
