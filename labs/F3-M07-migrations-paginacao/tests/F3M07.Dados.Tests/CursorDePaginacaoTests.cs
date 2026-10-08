using F3M07.Dados.Paginacao;

namespace F3M07.Dados.Tests;

/// <summary>Passo 7: cursor opaco (sem banco).</summary>
public sealed class CursorDePaginacaoTests
{
    [Fact]
    public void Codificar_DepoisDecodificar_VoltaAMesmaPosicaoComPrecisaoDeTick()
    {
        var posicao = new PosicaoCursor(new DateTime(2026, 3, 1, 12, 34, 56).AddTicks(1234567), 42);

        var cursor = CursorDePaginacao.Codificar(posicao);

        CursorDePaginacao.Decodificar(cursor).ShouldBe(posicao);
    }

    [Fact]
    public void Codificar_QualquerPosicao_GeraTextoSeguroParaQueryStringEQueNaoExpoeOsValores()
    {
        var cursor = CursorDePaginacao.Codificar(new PosicaoCursor(DateTime.MaxValue, int.MaxValue));

        cursor.ShouldNotBeNullOrWhiteSpace();
        cursor.ShouldAllBe(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_'); // Base64Url, sem + / =
        cursor.ShouldNotContain(int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("isto não é base64!")]
    [InlineData("AAAA")]                 // Base64 válido, tamanho errado
    [InlineData("AgAAAAAAAAAAAAAAAQ")]   // 13 bytes, versão de formato 2 (desconhecida)
    [InlineData("AQAAAAAAAAAAAAAAAA")]   // 13 bytes, versão 1, mas Id = 0
    public void Decodificar_TextoQueNaoFoiGeradoPorNos_LancaCursorInvalido(string cursor)
    {
        Should.Throw<CursorInvalidoException>(() => CursorDePaginacao.Decodificar(cursor));
    }
}
