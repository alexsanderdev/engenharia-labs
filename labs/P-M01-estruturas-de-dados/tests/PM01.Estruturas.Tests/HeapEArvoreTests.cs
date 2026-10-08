namespace PM01.Estruturas.Tests;

public class MinHeapTests
{
    [Fact]
    public void Remover_SempreDevolveOMenor()
    {
        var heap = new MinHeap<int>();
        foreach (var n in new[] { 5, 3, 8, 1, 9, 2, 7 })
            heap.Inserir(n);

        heap.Espiar().ShouldBe(1);

        var saida = new List<int>();
        while (heap.Count > 0)
            saida.Add(heap.Remover());

        saida.ShouldBe([1, 2, 3, 5, 7, 8, 9]);
    }

    [Fact]
    public void Inserir_ValoresRepetidos_DevolveTodos()
    {
        var heap = new MinHeap<int>();
        foreach (var n in new[] { 4, 4, 1, 4, 1 })
            heap.Inserir(n);

        var saida = new List<int>();
        while (heap.Count > 0)
            saida.Add(heap.Remover());

        saida.ShouldBe([1, 1, 4, 4, 4]);
    }

    [Fact]
    public void ComComparador_FuncionaComoFilaDePrioridadeDePedidos()
    {
        // Prioridade menor = atende primeiro (ex.: 0 = cliente VIP).
        var fila = new MinHeap<(string Pedido, int Prioridade)>(
            Comparer<(string Pedido, int Prioridade)>.Create((a, b) => a.Prioridade.CompareTo(b.Prioridade)));

        fila.Inserir(("PED-1", 2));
        fila.Inserir(("PED-2", 0));
        fila.Inserir(("PED-3", 1));

        fila.Remover().Pedido.ShouldBe("PED-2");
        fila.Remover().Pedido.ShouldBe("PED-3");
        fila.Remover().Pedido.ShouldBe("PED-1");
    }

    [Fact]
    public void Remover_HeapVazio_LancaInvalidOperation()
    {
        var heap = new MinHeap<int>();

        Should.Throw<InvalidOperationException>(() => heap.Remover());
        Should.Throw<InvalidOperationException>(() => heap.Espiar());
    }
}

public class ArvoreBinariaDeBuscaTests
{
    [Fact]
    public void EmOrdem_DevolveItensOrdenados()
    {
        var arvore = new ArvoreBinariaDeBusca<int>();
        foreach (var n in new[] { 50, 30, 70, 20, 40, 60, 80 })
            arvore.Inserir(n);

        arvore.EmOrdem().ShouldBe([20, 30, 40, 50, 60, 70, 80]);
        arvore.Count.ShouldBe(7);
    }

    [Fact]
    public void Inserir_Duplicado_RetornaFalseENaoAltera()
    {
        var arvore = new ArvoreBinariaDeBusca<string>();

        arvore.Inserir("m").ShouldBeTrue();
        arvore.Inserir("m").ShouldBeFalse();
        arvore.Count.ShouldBe(1);
    }

    [Fact]
    public void Contem_MinimoEMaximo()
    {
        var arvore = new ArvoreBinariaDeBusca<int>();
        foreach (var n in new[] { 8, 3, 10, 1, 6, 14 })
            arvore.Inserir(n);

        arvore.Contem(6).ShouldBeTrue();
        arvore.Contem(7).ShouldBeFalse();
        arvore.Minimo().ShouldBe(1);
        arvore.Maximo().ShouldBe(14);
    }

    [Fact]
    public void Altura_InsercaoOrdenada_DegeneraEmLista()
    {
        var balanceada = new ArvoreBinariaDeBusca<int>();
        foreach (var n in new[] { 4, 2, 6, 1, 3, 5, 7 })
            balanceada.Inserir(n);

        var degenerada = new ArvoreBinariaDeBusca<int>();
        for (var n = 1; n <= 7; n++)
            degenerada.Inserir(n);

        // Árvore vazia tem altura 0; um único nó tem altura 1.
        new ArvoreBinariaDeBusca<int>().Altura.ShouldBe(0);
        balanceada.Altura.ShouldBe(3);
        degenerada.Altura.ShouldBe(7);
    }

    [Theory]
    [InlineData(20)] // folha
    [InlineData(30)] // um filho
    [InlineData(50)] // dois filhos (raiz)
    public void Remover_TresCasos_MantemOrdem(int alvo)
    {
        var arvore = new ArvoreBinariaDeBusca<int>();
        foreach (var n in new[] { 50, 30, 70, 20, 60, 80 })
            arvore.Inserir(n);

        arvore.Remover(alvo).ShouldBeTrue();

        arvore.Contem(alvo).ShouldBeFalse();
        arvore.Count.ShouldBe(5);
        arvore.EmOrdem().ShouldBe(new[] { 20, 30, 50, 60, 70, 80 }.Where(n => n != alvo));
    }

    [Fact]
    public void MinimoEmArvoreVazia_LancaInvalidOperation()
    {
        var arvore = new ArvoreBinariaDeBusca<int>();

        Should.Throw<InvalidOperationException>(() => arvore.Minimo());
        arvore.Remover(1).ShouldBeFalse();
        arvore.EmOrdem().ShouldBeEmpty();
    }
}
