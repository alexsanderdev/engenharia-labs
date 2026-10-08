namespace F1M05.Memoria.Tests;

public class EtiquetasTests
{
    private static readonly CodigoPedido Codigo = new(TipoCodigo.Pedido, 2026, 123);

    [Fact]
    public void MontarEtiqueta_NomeCurto_UsaStackallocENaoTocaNoPool()
    {
        var pool = new PoolContador();

        var etiqueta = Etiquetas.MontarEtiqueta(Codigo, "  Ana Silva ", pool);

        etiqueta.ShouldBe("PED-2026-000123 | ANA SILVA");
        pool.Alugueis.ShouldBe(0);
    }

    [Fact]
    public void MontarEtiqueta_NomeLongo_AlugaEDevolveAoPool()
    {
        var pool = new PoolContador();
        var nome = new string('b', 1000);

        var etiqueta = Etiquetas.MontarEtiqueta(Codigo, nome, pool);

        etiqueta.Length.ShouldBe(15 + 3 + 1000);
        etiqueta.ShouldEndWith(new string('B', 1000));
        pool.Alugueis.ShouldBe(1);
        pool.Pendentes.ShouldBe(0, "todo array alugado precisa voltar ao pool");
    }

    [Fact]
    public void MontarEtiqueta_AlocaApenasAStringFinal()
    {
        // string de 27 chars = ~80 bytes no x64; damos folga, mas uma string intermediária estouraria o limite.
        var bytes = Alocacao.Medir(() => Etiquetas.MontarEtiqueta(Codigo, "Ana Silva"), repeticoes: 1);

        bytes.ShouldBeLessThanOrEqualTo(100);
    }
}
