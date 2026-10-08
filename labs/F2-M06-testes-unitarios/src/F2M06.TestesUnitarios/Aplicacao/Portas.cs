using F2M06.TestesUnitarios.Dominio;

namespace F2M06.TestesUnitarios.Aplicacao;

/// <summary>
/// Repositório de pedidos. Dependência GERENCIADA (o banco é só nosso, ninguém de fora o enxerga):
/// nos testes unitários use um FAKE em memória e verifique ESTADO, não chamadas.
/// </summary>
public interface IRepositorioDePedidos
{
    Task AdicionarAsync(Pedido pedido, CancellationToken cancellationToken);
    Task<Pedido?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<int> ContarEmAbertoDoClienteAsync(Guid clienteId, CancellationToken cancellationToken);
}

/// <summary>Catálogo de produtos (consulta). Nos testes, um STUB: só devolve dados, nunca é verificado.</summary>
public interface ICatalogoDeProdutos
{
    Task<Produto?> ObterPorIdAsync(Guid produtoId, CancellationToken cancellationToken);
}

/// <summary>
/// Publicador de eventos para outros sistemas (Service Bus, por exemplo). Dependência NÃO GERENCIADA:
/// a mensagem é um contrato observável de fora. Aqui SIM vale um MOCK que verifica a interação.
/// </summary>
public interface IPublicadorDeEventos
{
    Task PublicarAsync(PedidoCriado evento, CancellationToken cancellationToken);
}

/// <summary>Evento de integração publicado quando um pedido é criado.</summary>
public sealed record PedidoCriado(Guid PedidoId, Guid ClienteId, decimal Total, DateTimeOffset CriadoEm);
