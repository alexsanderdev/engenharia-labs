using F5M01.Api.Dominio;
using F5M01.Api.Http;

namespace F5M01.Api.Tests;

/// <summary>Passo 2: ETag e pré-condições (RFC 9110). Funções puras, sem HTTP.</summary>
public sealed class ETagsTests
{
    [Fact]
    public void Para_UsaAVersaoDoPedidoEntreAspas_EMudaQuandoOEstadoMuda()
    {
        var pedido = Pedido.Criar(Guid.NewGuid(), DateTimeOffset.UnixEpoch);
        ETags.Para(pedido).ShouldBe("\"1\"");

        pedido.DefinirItem(ProdutosConhecidos.Teclado, 1, out _);
        ETags.Para(pedido).ShouldBe("\"2\"");

        pedido.DefinirItem(ProdutosConhecidos.Teclado, 1, out _); // mesma quantidade: nada muda
        ETags.Para(pedido).ShouldBe("\"2\"");
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("\"3\"", true)]
    [InlineData("W/\"3\"", true)]          // If-None-Match usa comparação FRACA
    [InlineData("\"1\", \"3\"", true)]     // lista
    [InlineData("*", true)]
    [InlineData("\"2\"", false)]
    [InlineData("3", false)]               // sem aspas não é o mesmo ETag
    public void IfNoneMatchCorresponde_ComparacaoFraca(string? header, bool esperado) =>
        ETags.IfNoneMatchCorresponde(header, "\"3\"").ShouldBe(esperado);

    [Theory]
    [InlineData(null, true)]               // sem pré-condição
    [InlineData("", true)]
    [InlineData("\"3\"", true)]
    [InlineData("*", true)]
    [InlineData("\"2\", \"3\"", true)]
    [InlineData("W/\"3\"", false)]         // If-Match usa comparação FORTE: ETag fraco nunca corresponde
    [InlineData("\"2\"", false)]
    public void IfMatchAtendido_ComparacaoForte(string? header, bool esperado) =>
        ETags.IfMatchAtendido(header, "\"3\"").ShouldBe(esperado);
}
