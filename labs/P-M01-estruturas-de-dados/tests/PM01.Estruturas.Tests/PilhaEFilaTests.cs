namespace PM01.Estruturas.Tests;

public class PilhaTests
{
    [Fact]
    public void Empilhar_Desempilhar_SegueLifo()
    {
        var pilha = new Pilha<int>();
        pilha.Empilhar(1);
        pilha.Empilhar(2);
        pilha.Empilhar(3);

        pilha.Topo().ShouldBe(3);
        pilha.Desempilhar().ShouldBe(3);
        pilha.Desempilhar().ShouldBe(2);
        pilha.Count.ShouldBe(1);
    }

    [Fact]
    public void Desempilhar_PilhaVazia_LancaInvalidOperation()
    {
        var pilha = new Pilha<string>();

        Should.Throw<InvalidOperationException>(() => pilha.Desempilhar());
        Should.Throw<InvalidOperationException>(() => pilha.Topo());
        pilha.TentarDesempilhar(out _).ShouldBeFalse();
    }
}

public class FilaCircularTests
{
    [Fact]
    public void Enfileirar_Desenfileirar_SegueFifo()
    {
        var fila = new FilaCircular<string>(capacidadeInicial: 4);
        fila.Enfileirar("pedido-1");
        fila.Enfileirar("pedido-2");

        fila.Espiar().ShouldBe("pedido-1");
        fila.Desenfileirar().ShouldBe("pedido-1");
        fila.Desenfileirar().ShouldBe("pedido-2");
        fila.Count.ShouldBe(0);
    }

    [Fact]
    public void Enfileirar_DepoisDeDarAVolta_MantemOrdem()
    {
        var fila = new FilaCircular<int>(capacidadeInicial: 4);
        fila.Enfileirar(1);
        fila.Enfileirar(2);
        fila.Enfileirar(3);
        fila.Desenfileirar(); // libera a posição 0 do array
        fila.Desenfileirar(); // libera a posição 1
        fila.Enfileirar(4);
        fila.Enfileirar(5);   // cauda deu a volta para o índice 0
        fila.Enfileirar(6);   // e para o índice 1

        fila.Capacity.ShouldBe(4, "ainda cabe sem crescer: a fila reaproveita as posições liberadas");
        fila.ToArray().ShouldBe([3, 4, 5, 6]);
    }

    [Fact]
    public void Enfileirar_FilaCheiaComVolta_CresceEPreservaOrdem()
    {
        var fila = new FilaCircular<int>(capacidadeInicial: 4);
        fila.Enfileirar(1);
        fila.Enfileirar(2);
        fila.Enfileirar(3);
        fila.Desenfileirar();
        fila.Enfileirar(4);
        fila.Enfileirar(5); // cheia, com a cauda "antes" da cabeça no array

        fila.Enfileirar(6); // força o crescimento

        fila.Capacity.ShouldBe(8);
        var saida = new List<int>();
        while (fila.Count > 0)
            saida.Add(fila.Desenfileirar());
        saida.ShouldBe([2, 3, 4, 5, 6]);
    }

    [Fact]
    public void Desenfileirar_FilaVazia_LancaInvalidOperation()
    {
        var fila = new FilaCircular<int>();

        Should.Throw<InvalidOperationException>(() => fila.Desenfileirar());
    }
}
