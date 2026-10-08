using F2M06.TestesUnitarios.Aplicacao;
using F2M06.TestesUnitarios.Dominio;

namespace F2M06.TestesUnitarios.Tests.Dubles;

/// <summary>
/// FAKE: implementação de verdade, só que em memória. Diferente de um mock, ele TEM comportamento
/// (guarda, busca, conta), então o teste verifica o ESTADO final ("o pedido está salvo?")
/// e não a conversa ("AdicionarAsync foi chamado?").
/// </summary>
/// <remarks>TODO (Passo 3): use um Dictionary&lt;Guid, Pedido&gt; privado.</remarks>
public sealed class RepositorioDePedidosEmMemoria : IRepositorioDePedidos
{
    /// <summary>Cria o fake já com pedidos existentes (o "estado do banco" antes do teste).</summary>
    public RepositorioDePedidosEmMemoria(params Pedido[] existentes)
    {
        // TODO: guarde os pedidos existentes (quando o dicionário existir).
        _ = existentes;
    }

    /// <summary>Tudo o que está "no banco" — útil para asserts como <c>Pedidos.ShouldBeEmpty()</c>.</summary>
    public IReadOnlyCollection<Pedido> Pedidos =>
        throw new NotImplementedException("TODO: devolva os valores do dicionário.");

    public Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken) =>
        throw new NotImplementedException("TODO: adicione pelo Id (id duplicado deve lançar, como uma PK) e devolva Task.CompletedTask.");

    public Task<Pedido?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotImplementedException("TODO: Task.FromResult com o pedido ou null.");

    public Task<int> ContarEmAbertoDoClienteAsync(Guid clienteId, CancellationToken cancellationToken) =>
        throw new NotImplementedException("TODO: conte pedidos do cliente com EmAberto == true.");
}
