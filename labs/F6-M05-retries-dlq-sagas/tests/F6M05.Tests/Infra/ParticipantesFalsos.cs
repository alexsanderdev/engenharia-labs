using System.Collections.Concurrent;
using F6M05.Sagas.Mensageria;
using F6M05.Sagas.Saga;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace F6M05.Tests.Infra;

/// <summary>
/// PRONTA. Os outros serviços da saga, falsos mas falando AMQP de verdade: assinam os comandos no
/// exchange da saga e respondem com eventos na fila de eventos da saga.
/// <list type="bullet">
/// <item>Estoque: <c>ReservarEstoque</c> → <c>EstoqueReservado</c>; <c>LiberarEstoque</c> → <c>EstoqueLiberado</c>.</item>
/// <item>Pagamento: <c>AutorizarPagamento</c> → autorizado se valor ≤ limite, senão recusado; <c>EstornarPagamento</c> só registra.</item>
/// <item>Pedidos: <c>ConfirmarPedido</c>/<c>CancelarPedido</c> só registram.</item>
/// </list>
/// Usam um consumidor AMQP cru (sem o <c>ConsumidorComRetry</c> do aluno), para o teste ponta a
/// ponta depender só do que está sendo testado.
/// </summary>
public sealed class ParticipantesFalsos : IAsyncDisposable
{
    private readonly List<IChannel> _canais = [];
    private CanalDePublicacao? _publicacao;

    /// <summary>Todos os comandos recebidos, por qualquer participante, em ordem de chegada.</summary>
    public ConcurrentQueue<ComandoSaga> Recebidos { get; } = new();

    public IReadOnlyList<ComandoSaga> DoPedido(Guid pedidoId) => [.. Recebidos.Where(c => c.PedidoId == pedidoId)];

    public static async Task<ParticipantesFalsos> IniciarAsync(IConnection conexao, NomesDaSaga nomes, decimal limiteDoPagamento)
    {
        var p = new ParticipantesFalsos { _publicacao = await CanalDePublicacao.CriarAsync(conexao) };
        var canal = p._publicacao.Canal;
        await canal.ExchangeDeclareAsync(nomes.ExchangeDeComandos, ExchangeType.Direct, durable: true, autoDelete: false);

        await p.AssinarAsync(conexao, nomes, "estoque", [nameof(ReservarEstoque), nameof(LiberarEstoque)], c => c switch
        {
            ReservarEstoque r => new EstoqueReservado($"{r.PedidoId:N}:EstoqueReservado", r.PedidoId),
            LiberarEstoque l => new EstoqueLiberado($"{l.PedidoId:N}:EstoqueLiberado", l.PedidoId),
            _ => null,
        });
        await p.AssinarAsync(conexao, nomes, "pagamento", [nameof(AutorizarPagamento), nameof(EstornarPagamento)], c => c switch
        {
            AutorizarPagamento a when a.Valor <= limiteDoPagamento =>
                new PagamentoAutorizado($"{a.PedidoId:N}:PagamentoAutorizado", a.PedidoId, $"aut-{a.PedidoId:N}"),
            AutorizarPagamento a => new PagamentoRecusado($"{a.PedidoId:N}:PagamentoRecusado", a.PedidoId, "limite excedido"),
            _ => null,
        });
        await p.AssinarAsync(conexao, nomes, "pedidos", [nameof(ConfirmarPedido), nameof(CancelarPedido)], _ => null);
        return p;
    }

    private async Task AssinarAsync(IConnection conexao, NomesDaSaga nomes, string servico, string[] comandos, Func<ComandoSaga, MensagemSaga?> responder)
    {
        var canal = await conexao.CreateChannelAsync();
        _canais.Add(canal);
        var fila = $"{nomes.Prefixo}.{servico}";
        await canal.QueueDeclareAsync(fila, durable: false, exclusive: false, autoDelete: true);
        foreach (var comando in comandos)
            await canal.QueueBindAsync(fila, nomes.ExchangeDeComandos, comando);

        var consumidor = new AsyncEventingBasicConsumer(canal);
        consumidor.ReceivedAsync += async (_, entrega) =>
        {
            var comando = Serializador.Desserializar<ComandoSaga>(entrega.Body.ToArray());
            Recebidos.Enqueue(comando);
            if (responder(comando) is { } evento)
                await _publicacao!.PublicarJsonAsync<MensagemSaga>("", nomes.FilaDeEventos, evento, evento.MessageId, evento.Tipo, evento.PedidoId.ToString());
            await canal.BasicAckAsync(entrega.DeliveryTag, multiple: false);
        };
        await canal.BasicConsumeAsync(fila, autoAck: false, consumidor);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var canal in _canais) await canal.DisposeAsync();
        if (_publicacao is not null) await _publicacao.DisposeAsync();
    }
}
