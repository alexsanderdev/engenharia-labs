using Microsoft.Extensions.Time.Testing;

namespace F2M05.Tdd.Tests.MeusTestes;

/// <summary>
/// SEUS testes unitários, escritos em ciclos red-green-refactor (ver "Roteiro de ciclos" na nota Lab).
/// Regras do jogo:
///   1. Escreva UM teste e veja-o falhar pelo motivo certo (red).
///   2. Escreva o MÍNIMO de código em src/ para ele passar (green). Vale "fake it".
///   3. Refatore código e teste com tudo verde (refactor). Faça commit.
///   4. Volte ao 1 com o próximo teste do roteiro.
/// </summary>
public sealed class PedidoTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 8, 14, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _relogio = new(Agora);

    // Ciclo 1 — comece por aqui. Apague este comentário quando escrever o primeiro teste.
    // [Fact]
    // public void NovoPedido_ComecaEmCreated()
    // {
    //     var pedido = new Pedido(_relogio);
    //
    //     pedido.Status.ShouldBe(StatusPedido.Created);
    // }
}
