namespace F1M02.Colecoes.Tests;

// Passo 1 — Repositório genérico em memória (Dictionary + constraints).
public class RepositorioTests
{
    private static Produto NovoProduto(string nome = "X-Burger", decimal preco = 30m, string categoria = "Lanches") =>
        new(Guid.NewGuid(), nome, preco, categoria);

    [Fact]
    public void Adicionar_DepoisObterPorId_RetornaAMesmaEntidade()
    {
        var repo = new RepositorioEmMemoria<Produto, Guid>();
        var burger = NovoProduto();

        repo.Adicionar(burger).ShouldBeTrue();

        repo.ObterPorId(burger.Id).ShouldBe(burger);
        repo.ObterPorId(Guid.NewGuid()).ShouldBeNull();
        repo.Quantidade.ShouldBe(1);
    }

    [Fact]
    public void Adicionar_IdDuplicado_RetornaFalseSemSobrescrever()
    {
        var repo = new RepositorioEmMemoria<Produto, Guid>();
        var burger = NovoProduto();
        repo.Adicionar(burger);

        repo.Adicionar(burger with { Nome = "Outro" }).ShouldBeFalse();

        repo.ObterPorId(burger.Id)!.Nome.ShouldBe("X-Burger");
        Should.Throw<ArgumentNullException>(() => repo.Adicionar(null!));
    }

    [Fact]
    public void AtualizarERemover_RespeitamExistencia()
    {
        var repo = new RepositorioEmMemoria<Produto, Guid>();
        var burger = NovoProduto();
        repo.Adicionar(burger);

        repo.Atualizar(burger with { Preco = 35m });
        repo.ObterPorId(burger.Id)!.Preco.ShouldBe(35m);
        Should.Throw<KeyNotFoundException>(() => repo.Atualizar(NovoProduto()));

        repo.Remover(burger.Id).ShouldBeTrue();
        repo.Remover(burger.Id).ShouldBeFalse();
        repo.Quantidade.ShouldBe(0);
    }

    [Fact]
    public void ListarEBuscar_RetornamSnapshots()
    {
        var repo = new RepositorioEmMemoria<Produto, Guid>();
        repo.Adicionar(NovoProduto("Suco", 8m, "Bebidas"));
        repo.Adicionar(NovoProduto("X-Burger", 30m));

        var lista = repo.Listar();
        var bebidas = repo.Buscar(p => p.Categoria == "Bebidas");
        repo.Adicionar(NovoProduto("Água", 4m, "Bebidas"));

        lista.Count.ShouldBe(2);
        bebidas.Select(p => p.Nome).ShouldBe(["Suco"]);
        repo.Listar().Count.ShouldBe(3);
    }

    [Fact]
    public void Repositorio_ComIdStringEComparador_ReutilizaOMesmoCodigo()
    {
        var repo = new RepositorioEmMemoria<Cliente, string>(StringComparer.OrdinalIgnoreCase);
        repo.Adicionar(new Cliente("ana@orderflow.dev", "Ana"));

        repo.ObterPorId("ANA@ORDERFLOW.DEV")!.Nome.ShouldBe("Ana");
        repo.Adicionar(new Cliente("Ana@OrderFlow.dev", "Ana 2")).ShouldBeFalse();
    }

    [Fact]
    public void RepositorioLeitura_ECovarianteNaEntidade()
    {
        var repo = new RepositorioEmMemoria<Produto, Guid>();
        repo.Adicionar(NovoProduto(preco: 10m));
        repo.Adicionar(NovoProduto(preco: 15.5m));

        // Só compila porque a interface declara "out TEntidade".
#pragma warning disable CA1859 // aqui o tipo da interface é justamente o ponto do teste
        IRepositorioLeitura<IComPreco, Guid> leitura = repo;
#pragma warning restore CA1859

        Relatorios.SomarPrecos(leitura.Listar()).ShouldBe(25.5m);
    }
}

// Passo 2 — Relatórios genéricos e variância de IEnumerable<out T>.
public class RelatoriosTests
{
    private static readonly List<Produto> Produtos =
    [
        new(Guid.NewGuid(), "Suco", 8m, "Bebidas"),
        new(Guid.NewGuid(), "Pizza", 50m, "Pizzas"),
        new(Guid.NewGuid(), "Calabresa", 50m, "Pizzas"),
        new(Guid.NewGuid(), "Água", 4m, "Bebidas"),
    ];

    [Fact]
    public void SomarPrecosEMaisCaro_AceitamListaDeProduto()
    {
        Relatorios.SomarPrecos(Produtos).ShouldBe(112m);
        Relatorios.SomarPrecos([]).ShouldBe(0m);

        Produto? maisCaro = Relatorios.MaisCaro(Produtos);
        maisCaro!.Nome.ShouldBe("Pizza");
        Relatorios.MaisCaro(new List<Produto>()).ShouldBeNull();
    }

    [Fact]
    public void AgruparPor_PreservaOrdemDentroDoGrupo()
    {
        var grupos = Relatorios.AgruparPor(Produtos, p => p.Categoria);

        grupos.Count.ShouldBe(2);
        grupos["Bebidas"].Select(p => p.Nome).ShouldBe(["Suco", "Água"]);
        grupos["Pizzas"].Select(p => p.Nome).ShouldBe(["Pizza", "Calabresa"]);
    }
}
