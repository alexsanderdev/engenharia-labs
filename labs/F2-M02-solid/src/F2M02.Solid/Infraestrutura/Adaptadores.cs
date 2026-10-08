using System.Globalization;
using System.Text.Json;

using F2M02.Solid.Aplicacao;
using F2M02.Solid.Dominio;

namespace F2M02.Solid.Infraestrutura;

// Adaptadores: implementam as portas da aplicação usando a infraestrutura concreta.
// A dependência aponta de fora para dentro: Infraestrutura conhece Aplicação, nunca o contrário (DIP).

/// <summary>Um adaptador, DUAS interfaces pequenas: escrita e leitura (ISP não exige uma classe por interface).</summary>
public sealed class RepositorioSqlDePedidos(BancoDeDadosSql banco) : IRepositorioDePedidos, ILeitorDePedidos
{
    public Task SalvarAsync(Pedido pedido, CancellationToken ct)
    {
        banco.InserirPedido(pedido);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Pedido>> ListarDoClienteAsync(Guid clienteId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Pedido>>([.. banco.Pedidos.Where(p => p.ClienteId == clienteId)]);
}

public sealed class CatalogoSqlDeProdutos(BancoDeDadosSql banco) : ICatalogoDeProdutos
{
    public Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Produto>>([.. ids.Select(banco.BuscarProduto).OfType<Produto>()]);
}

/// <summary>Mantém exatamente o e-mail do legado: assunto "Pedido recebido" e total com ponto decimal.</summary>
public sealed class NotificadorPorEmail(ServidorSmtp smtp) : INotificadorDeCliente
{
    public Task NotificarPedidoCriadoAsync(Pedido pedido, string emailCliente, CancellationToken ct)
    {
        var total = pedido.Total.ToString("F2", CultureInfo.InvariantCulture);
        smtp.Enviar(emailCliente, "Pedido recebido", $"Olá! Seu pedido {pedido.Id} foi criado. Total: R$ {total}");
        return Task.CompletedTask;
    }
}

/// <summary>Publica no tópico <see cref="Topico"/>, chave = Id do pedido, valor = JSON { Id, ClienteId, Total }.</summary>
public sealed class PublicadorKafkaDePedidos(ProdutorKafka kafka) : IOrderEventPublisher
{
    public const string Topico = "pedidos.criados";

    public Task PublishOrderCreatedAsync(Pedido pedido, CancellationToken ct)
    {
        var valor = JsonSerializer.Serialize(new { pedido.Id, pedido.ClienteId, pedido.Total });
        kafka.Produzir(Topico, pedido.Id.ToString(), valor);
        return Task.CompletedTask;
    }
}
