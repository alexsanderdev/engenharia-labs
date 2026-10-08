namespace F2M05.Tdd.Tests.MeusTestes;

/// <summary>
/// Nasceu no refactor do ciclo 13, junto com <see cref="RegrasDeTransicao"/>.
/// A tabela inteira (4 × 4 = 16 pares) documenta a máquina de estados num lugar só.
/// </summary>
public sealed class RegrasDeTransicaoTests
{
    [Theory]
    [InlineData(StatusPedido.Created, StatusPedido.Created, false)]
    [InlineData(StatusPedido.Created, StatusPedido.Confirmed, true)]
    [InlineData(StatusPedido.Created, StatusPedido.Completed, false)]
    [InlineData(StatusPedido.Created, StatusPedido.Cancelled, true)]
    [InlineData(StatusPedido.Confirmed, StatusPedido.Created, false)]
    [InlineData(StatusPedido.Confirmed, StatusPedido.Confirmed, false)]
    [InlineData(StatusPedido.Confirmed, StatusPedido.Completed, true)]
    [InlineData(StatusPedido.Confirmed, StatusPedido.Cancelled, false)]
    [InlineData(StatusPedido.Completed, StatusPedido.Created, false)]
    [InlineData(StatusPedido.Completed, StatusPedido.Confirmed, false)]
    [InlineData(StatusPedido.Completed, StatusPedido.Completed, false)]
    [InlineData(StatusPedido.Completed, StatusPedido.Cancelled, false)]
    [InlineData(StatusPedido.Cancelled, StatusPedido.Created, false)]
    [InlineData(StatusPedido.Cancelled, StatusPedido.Confirmed, false)]
    [InlineData(StatusPedido.Cancelled, StatusPedido.Completed, false)]
    [InlineData(StatusPedido.Cancelled, StatusPedido.Cancelled, false)]
    public void Permitida_TabelaCompleta(StatusPedido de, StatusPedido para, bool esperado)
    {
        RegrasDeTransicao.Permitida(de, para).ShouldBe(esperado);
    }
}
