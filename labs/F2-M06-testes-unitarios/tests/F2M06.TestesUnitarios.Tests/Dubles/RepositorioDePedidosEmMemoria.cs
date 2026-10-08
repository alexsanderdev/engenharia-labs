using F2M06.TestesUnitarios.Aplicacao;
using F2M06.TestesUnitarios.Dominio;

namespace F2M06.TestesUnitarios.Tests.Dubles;

/// <summary>
/// FAKE: implementação de verdade, só que em memória. Diferente de um mock, ele TEM comportamento
/// (guarda, busca, conta), então o teste verifica o ESTADO final ("o pedido está salvo?")
/// e não a conversa ("AdicionarAsync foi chamado?"). Se amanhã o caso de uso salvar de outro jeito
/// (ex.: AdicionarVariosAsync), os testes continuam válidos.
/// </summary>
public sealed class RepositorioDePedidosEmMemoria : IRepositorioDePedidos
{
    private readonly Dictionary<Guid, Pedido> _pedidos = [];

    /// <summary>Cria o fake já com pedidos existentes (o "estado do banco" antes do teste).</summary>
    public RepositorioDePedidosEmMemoria(params Pedido[] existentes)
    {
        foreach (var pedido in existentes)
            _pedidos.Add(pedido.Id, pedido);
    }

    /// <summary>Tudo o que está "no banco" — útil para asserts como <c>Pedidos.ShouldBeEmpty()</c>.</summary>
    public IReadOnlyCollection<Pedido> Pedidos => _pedidos.Values;

    public Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        _pedidos.Add(pedido.Id, pedido); // Add (não indexer): id duplicado deve explodir, como uma PK no banco.
        return Task.CompletedTask;
    }

    public Task<Pedido?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_pedidos.GetValueOrDefault(id));

    public Task<int> ContarEmAbertoDoClienteAsync(Guid clienteId, CancellationToken cancellationToken) =>
        Task.FromResult(_pedidos.Values.Count(p => p.ClienteId == clienteId && p.EmAberto));
}
