namespace F1M04.Linq.Tests;

public class ConsultasCatalogoTests
{
    [Fact]
    public void AtivosDaCategoria_IgnoraInativosEMaiusculas_OrdenaPorNome()
    {
        var resultado = ConsultasCatalogo.AtivosDaCategoria(Dados.Produtos(), "livros");

        resultado.Select(p => p.Nome).ShouldBe(["Livro Clean Code", "Livro Refactoring"]);
    }

    [Fact]
    public void Buscar_SemFiltros_DevolvePrimeiraPaginaOrdenadaPorPrecoENome()
    {
        var pagina = ConsultasCatalogo.Buscar(Dados.Produtos(), new FiltroCatalogo { TamanhoPagina = 3 });

        pagina.Itens.Select(p => p.Nome).ShouldBe(["Caneca de café", "Luminária", "Mouse sem fio"]);
        pagina.TotalItens.ShouldBe(7); // 8 produtos, 1 inativo
        pagina.TotalPaginas.ShouldBe(3);
        pagina.Numero.ShouldBe(1);
    }

    [Fact]
    public void Buscar_ComTermoFaixaEInativos_DevolveSegundaPagina()
    {
        var filtro = new FiltroCatalogo
        {
            Termo = "LIVRO",
            ApenasAtivos = false,
            PrecoMinimo = 100m,
            PrecoMaximo = 500m,
            Pagina = 2,
            TamanhoPagina = 2,
        };

        var pagina = ConsultasCatalogo.Buscar(Dados.Produtos(), filtro);

        pagina.Itens.Select(p => p.Nome).ShouldBe(["Livro DDD"]);
        pagina.TotalItens.ShouldBe(3);
        pagina.TotalPaginas.ShouldBe(2);
    }

    [Fact]
    public void Buscar_PaginaAlemDoFim_DevolveListaVaziaComTotalCorreto()
    {
        var pagina = ConsultasCatalogo.Buscar(Dados.Produtos(), new FiltroCatalogo { Pagina = 5, TamanhoPagina = 10 });

        pagina.Itens.ShouldBeEmpty();
        pagina.TotalItens.ShouldBe(7);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Buscar_PaginacaoInvalida_LancaArgumentOutOfRange(int numero, int tamanho)
    {
        var filtro = new FiltroCatalogo { Pagina = numero, TamanhoPagina = tamanho };

        Should.Throw<ArgumentOutOfRangeException>(() => ConsultasCatalogo.Buscar(Dados.Produtos(), filtro));
    }

    [Fact]
    public void Buscar_EnumeraAFonteUmaUnicaVez()
    {
        // Count() + Skip/Take direto na consulta = 2 enumerações da fonte (2 idas ao banco, se fosse um banco).
        var fonte = new EnumeravelContador<Produto>(Dados.Produtos());

        ConsultasCatalogo.Buscar(fonte, new FiltroCatalogo { TamanhoPagina = 2 });

        fonte.Enumeracoes.ShouldBe(1);
    }

    [Fact]
    public void ContarPorCategoria_DevolveQuantidadePorCategoria()
    {
        var contagem = ConsultasCatalogo.ContarPorCategoria(Dados.Produtos());

        contagem.Count.ShouldBe(3);
        contagem["Livros"].ShouldBe(3);
        contagem["Eletrônicos"].ShouldBe(3);
        contagem["Casa"].ShouldBe(2);
    }

    [Fact]
    public void IndicePorCategoria_ChaveSemMaiusculas_ECategoriaInexistenteVazia()
    {
        var indice = ConsultasCatalogo.IndicePorCategoria(Dados.Produtos());

        indice["eletrônicos"].Count().ShouldBe(3);
        indice["Brinquedos"].ShouldBeEmpty(); // ILookup não lança KeyNotFoundException
    }

    [Fact]
    public void SkusEmLotes_DivideEmLotesDeTamanhoFixo()
    {
        var lotes = ConsultasCatalogo.SkusEmLotes(Dados.Produtos(), 3);

        lotes.Count.ShouldBe(3);
        lotes[0].ShouldBe(["LIV-001", "LIV-002", "LIV-003"]);
        lotes[2].Length.ShouldBe(2);
    }

    [Fact]
    public void FaixaDePreco_FuncionaComIQueryableEEmMemoria()
    {
        var expressao = ConsultasCatalogo.FaixaDePreco(90m, 150m);

        // IQueryable: a consulta guarda uma árvore de expressão que um provider (ex.: EF Core) traduziria para SQL.
        var consulta = Dados.Produtos().AsQueryable().Where(expressao);
        consulta.Expression.ToString().ShouldContain("Preco");
        consulta.Count().ShouldBe(4);

        // IEnumerable: compilamos a expressão para um delegate e executamos em memória.
        Dados.Produtos().Count(expressao.Compile()).ShouldBe(4);
    }
}
