using F2M02.Solid.Dominio;

namespace F2M02.Solid.Aplicacao;

// As abstrações abaixo existem porque cada uma tem um MOTIVO: são detalhes de infraestrutura
// (banco, SMTP, Kafka) que variam por ambiente e que precisamos trocar por fakes em teste (DIP).
// Todas são pequenas (ISP): quem só grava não depende de quem lista, e vice-versa.

/// <summary>Leitura de produtos para montar o pedido.</summary>
public interface ICatalogoDeProdutos
{
    /// <summary>Devolve os produtos encontrados (os inexistentes simplesmente não vêm).</summary>
    Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);
}

/// <summary>Escrita de pedidos — o que o caso de uso "criar pedido" precisa e nada mais.</summary>
public interface IRepositorioDePedidos
{
    Task SalvarAsync(Pedido pedido, CancellationToken ct);
}

/// <summary>Leitura de pedidos — usada por consultas/relatórios, não pela criação.</summary>
public interface ILeitorDePedidos
{
    Task<IReadOnlyList<Pedido>> ListarDoClienteAsync(Guid clienteId, CancellationToken ct);
}

/// <summary>Avisar o cliente (hoje e-mail; amanhã pode ser push/WhatsApp).</summary>
public interface INotificadorDeCliente
{
    Task NotificarPedidoCriadoAsync(Pedido pedido, string emailCliente, CancellationToken ct);
}

/// <summary>
/// Publicação de eventos de pedido (o exemplo da Aula). O caso de uso não sabe se o evento
/// vai para Kafka, memória ou log.
/// </summary>
public interface IOrderEventPublisher
{
    Task PublishOrderCreatedAsync(Pedido pedido, CancellationToken ct);
}
