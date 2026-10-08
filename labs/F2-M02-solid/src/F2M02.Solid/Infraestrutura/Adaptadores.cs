using F2M02.Solid.Aplicacao;
using F2M02.Solid.Dominio;

namespace F2M02.Solid.Infraestrutura;

// Adaptadores: implementam as portas da aplicação usando a infraestrutura concreta.
// A dependência aponta de fora para dentro: Infraestrutura conhece Aplicação, nunca o contrário (DIP).
// Copie o comportamento EXATO das etapas 2, 4, 5 e 6 do PedidoService legado.

/// <summary>Um adaptador, DUAS interfaces pequenas: escrita e leitura (ISP não exige uma classe por interface).</summary>
public sealed class RepositorioSqlDePedidos(BancoDeDadosSql banco) : IRepositorioDePedidos, ILeitorDePedidos
{
    public Task SalvarAsync(Pedido pedido, CancellationToken ct)
    {
        _ = banco;
        throw new NotImplementedException("TODO: grave com banco.InserirPedido");
    }

    public Task<IReadOnlyList<Pedido>> ListarDoClienteAsync(Guid clienteId, CancellationToken ct)
    {
        _ = banco;
        throw new NotImplementedException("TODO: filtre banco.Pedidos pelo ClienteId");
    }
}

public sealed class CatalogoSqlDeProdutos(BancoDeDadosSql banco) : ICatalogoDeProdutos
{
    public Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        _ = banco;
        throw new NotImplementedException("TODO: banco.BuscarProduto para cada id, descartando os não encontrados");
    }
}

/// <summary>Mantém exatamente o e-mail do legado: assunto "Pedido recebido" e total com ponto decimal.</summary>
public sealed class NotificadorPorEmail(ServidorSmtp smtp) : INotificadorDeCliente
{
    public Task NotificarPedidoCriadoAsync(Pedido pedido, string emailCliente, CancellationToken ct)
    {
        _ = smtp;
        throw new NotImplementedException("TODO: smtp.Enviar com o mesmo assunto e corpo da etapa 5 do legado");
    }
}

/// <summary>Publica no tópico <see cref="Topico"/>, chave = Id do pedido, valor = JSON { Id, ClienteId, Total }.</summary>
public sealed class PublicadorKafkaDePedidos(ProdutorKafka kafka) : IOrderEventPublisher
{
    public const string Topico = "pedidos.criados";

    public Task PublishOrderCreatedAsync(Pedido pedido, CancellationToken ct)
    {
        _ = kafka;
        throw new NotImplementedException($"TODO: kafka.Produzir no tópico {Topico}, como na etapa 6 do legado");
    }
}
