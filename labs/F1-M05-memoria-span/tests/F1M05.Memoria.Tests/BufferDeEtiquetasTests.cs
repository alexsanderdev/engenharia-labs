namespace F1M05.Memoria.Tests;

public class BufferDeEtiquetasTests
{
    [Fact]
    public void Adicionar_AcumulaUmCodigoPorLinha()
    {
        using var buffer = new BufferDeEtiquetas(pool: new PoolContador());

        buffer.Adicionar(new CodigoPedido(TipoCodigo.Pedido, 2026, 1));
        buffer.Adicionar(new CodigoPedido(TipoCodigo.Devolucao, 2026, 2));

        buffer.Quantidade.ShouldBe(2);
        buffer.Conteudo.ToString().ShouldBe("PED-2026-000001\nDEV-2026-000002\n");
    }

    [Fact]
    public void Adicionar_AlemDaCapacidade_CresceEDevolveOsArraysAntigos()
    {
        var pool = new PoolContador();
        var buffer = new BufferDeEtiquetas(capacidadeInicial: 16, pool: pool);

        for (var i = 1; i <= 100; i++)
            buffer.Adicionar(new CodigoPedido(TipoCodigo.Pedido, 2026, i));

        buffer.Conteudo.Length.ShouldBe(100 * 16);
        buffer.Conteudo[^16..].ToString().ShouldBe("PED-2026-000100\n");
        pool.Alugueis.ShouldBeGreaterThan(1);
        pool.Pendentes.ShouldBe(1, "só o array atual deve continuar emprestado");

        buffer.Dispose();
        pool.Pendentes.ShouldBe(0);
    }

    [Fact]
    public void Dispose_ChamadoDuasVezes_DevolveUmaVezSo()
    {
        var pool = new PoolContador();
        var buffer = new BufferDeEtiquetas(pool: pool);

        buffer.Dispose();
        buffer.Dispose(); // o PoolContador lança se o array voltar duas vezes

        pool.Devolucoes.ShouldBe(1);
    }

    [Fact]
    public void UsoDepoisDoDispose_LancaObjectDisposedException()
    {
        var buffer = new BufferDeEtiquetas(pool: new PoolContador());
        buffer.Dispose();

        Should.Throw<ObjectDisposedException>(() => buffer.Adicionar(new CodigoPedido(TipoCodigo.Pedido, 2026, 1)));
        Should.Throw<ObjectDisposedException>(() => buffer.Conteudo.Length);
    }
}
