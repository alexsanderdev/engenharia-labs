using System.Collections.Frozen;

namespace F1M02.Colecoes.Tests;

// Passo 3 — PriorityQueue estável.
public class FilaDePreparoTests
{
    [Fact]
    public void TentarRetirar_FilaVazia_RetornaFalse()
    {
        var fila = new FilaDePreparo();

        fila.TentarRetirar(out _).ShouldBeFalse();
        fila.Quantidade.ShouldBe(0);
    }

    [Fact]
    public void TentarRetirar_ExpressaSaiAntesDeNormalEAgendada()
    {
        var fila = new FilaDePreparo();
        Guid agendado = Guid.NewGuid(), normal = Guid.NewGuid(), expresso = Guid.NewGuid();

        fila.Enfileirar(agendado, PrioridadeDePreparo.Agendada);
        fila.Enfileirar(normal, PrioridadeDePreparo.Normal);
        fila.Enfileirar(expresso, PrioridadeDePreparo.Expressa);

        fila.Quantidade.ShouldBe(3);
        Retirar(fila).ShouldBe(expresso);
        Retirar(fila).ShouldBe(normal);
        Retirar(fila).ShouldBe(agendado);
    }

    [Fact]
    public void TentarRetirar_MesmaPrioridade_RespeitaOrdemDeChegada()
    {
        var fila = new FilaDePreparo();
        var ids = Enumerable.Range(0, 50).Select(_ => Guid.NewGuid()).ToList();
        foreach (var id in ids)
            fila.Enfileirar(id, PrioridadeDePreparo.Normal);

        var saida = new List<Guid>();
        while (fila.TentarRetirar(out var id))
            saida.Add(id);

        saida.ShouldBe(ids);
    }

    private static Guid Retirar(FilaDePreparo fila)
    {
        fila.TentarRetirar(out var id).ShouldBeTrue();
        return id;
    }
}

// Passo 4 — SortedDictionary + HashSet.
public class IndiceDeCategoriasTests
{
    [Fact]
    public void Adicionar_SemDuplicatasEIgnorandoMaiusculas()
    {
        var indice = new IndiceDeCategorias();
        var pizza = Guid.NewGuid();

        indice.Adicionar("Pizzas", pizza).ShouldBeTrue();
        indice.Adicionar("PIZZAS", pizza).ShouldBeFalse();
        indice.Adicionar("pizzas", Guid.NewGuid()).ShouldBeTrue();

        indice.ProdutosDa("Pizzas").Count.ShouldBe(2);
        indice.ProdutosDa("Sobremesas").ShouldBeEmpty();
        Should.Throw<ArgumentException>(() => indice.Adicionar(" ", pizza));
    }

    [Fact]
    public void Categorias_SaemEmOrdemAlfabetica()
    {
        var indice = new IndiceDeCategorias();
        indice.Adicionar("Pizzas", Guid.NewGuid());
        indice.Adicionar("bebidas", Guid.NewGuid());
        indice.Adicionar("Lanches", Guid.NewGuid());

        indice.Categorias().ShouldBe(["bebidas", "Lanches", "Pizzas"]);
    }
}

// Passo 5 — FrozenDictionary.
public class TabelaDeFreteTests
{
    [Fact]
    public void Tabela_EFrozenEImutavelEmRelacaoAOrigem()
    {
        var origem = new Dictionary<string, decimal> { ["SP"] = 10m, ["RJ"] = 15m };

        var frete = new TabelaDeFrete(origem);
        origem["MG"] = 20m;

        frete.Tabela.ShouldBeAssignableTo<FrozenDictionary<string, decimal>>();
        frete.Tabela.Count.ShouldBe(2);
    }

    [Fact]
    public void ValorPara_IgnoraMaiusculasELancaParaUfDesconhecida()
    {
        var frete = new TabelaDeFrete(new Dictionary<string, decimal> { ["SP"] = 10m, ["RJ"] = 15m });

        frete.ValorPara("sp").ShouldBe(10m);
        frete.TentarObter("Rj", out var rj).ShouldBeTrue();
        rj.ShouldBe(15m);
        frete.TentarObter("AM", out _).ShouldBeFalse();
        Should.Throw<KeyNotFoundException>(() => frete.ValorPara("AM"));
    }
}

// Passo 6 — Cache LRU (Dictionary + LinkedList).
public class CacheLruTests
{
    [Fact]
    public void Construtor_CapacidadeInvalida_Lanca()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new CacheLru<string, int>(0));
    }

    [Fact]
    public void Definir_CacheCheio_RemoveOMenosRecente()
    {
        var cache = new CacheLru<string, decimal>(2);
        cache.Definir("a", 1m);
        cache.Definir("b", 2m);

        cache.Definir("c", 3m);

        cache.Quantidade.ShouldBe(2);
        cache.TentarObter("a", out _).ShouldBeFalse();
        cache.ChavesPorUso().ShouldBe(["c", "b"]);
    }

    [Fact]
    public void TentarObter_RenovaOItemEOutroEhRemovido()
    {
        var cache = new CacheLru<string, decimal>(2);
        cache.Definir("a", 1m);
        cache.Definir("b", 2m);

        cache.TentarObter("a", out var a).ShouldBeTrue();
        a.ShouldBe(1m);
        cache.Definir("c", 3m);

        cache.TentarObter("b", out _).ShouldBeFalse();
        cache.ChavesPorUso().ShouldBe(["c", "a"]);
    }

    [Fact]
    public void Definir_ChaveExistente_AtualizaSemCrescer()
    {
        var cache = new CacheLru<string, decimal>(2);
        cache.Definir("a", 1m);
        cache.Definir("b", 2m);

        cache.Definir("a", 10m);
        cache.Definir("c", 3m);

        cache.Quantidade.ShouldBe(2);
        cache.TentarObter("a", out var a).ShouldBeTrue();
        a.ShouldBe(10m);
        cache.TentarObter("b", out _).ShouldBeFalse();
    }
}
