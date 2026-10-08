namespace F1M04.Linq.Tests;

public class ExecucaoAdiadaTests
{
    [Fact]
    public void AcimaDe_AoSerChamado_NaoEnumeraAFonte()
    {
        var fonte = new EnumeravelContador<Produto>(Dados.Produtos());

        var consulta = ExecucaoAdiada.AcimaDe(fonte, 100m);

        fonte.Enumeracoes.ShouldBe(0);
        consulta.Count().ShouldBe(5);
        fonte.Enumeracoes.ShouldBe(1);
    }

    [Fact]
    public void AcimaDe_RefleteMudancasFeitasNaFonteDepoisDaChamada()
    {
        var produtos = Dados.Produtos();
        var caros = ExecucaoAdiada.AcimaDe(produtos, 1000m);

        produtos.Add(new Produto(99, "ELE-099", "Notebook", "Eletrônicos", 7000m, true));

        caros.Select(p => p.Nome).ShouldBe(["Monitor 27", "Notebook"]);
    }

    [Fact]
    public void Estatisticas_CalculaTudoEnumerandoAFonteUmaUnicaVez()
    {
        var fonte = new EnumeravelContador<Produto>(Dados.Produtos());

        var estatisticas = ExecucaoAdiada.Estatisticas(fonte);

        estatisticas.ShouldBe(new EstatisticasCatalogo(8, 40m, 1500m, 317.50m));
        fonte.Enumeracoes.ShouldBe(1);
    }

    [Fact]
    public void Estatisticas_FonteVazia_DevolveZeros()
    {
        ExecucaoAdiada.Estatisticas([]).ShouldBe(new EstatisticasCatalogo(0, 0, 0, 0));
    }
}
