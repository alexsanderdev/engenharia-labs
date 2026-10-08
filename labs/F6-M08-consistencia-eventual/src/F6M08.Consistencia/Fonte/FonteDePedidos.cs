using F6M08.Consistencia.Dominio;
using F6M08.Consistencia.Mensageria;

namespace F6M08.Consistencia.Fonte;

/// <summary>Estado de um pedido na fonte da verdade (o lado de escrita).</summary>
public sealed record PedidoNaFonte(
    Guid PedidoId, Guid ClienteId, StatusPedido Status, decimal Total, long Versao, DateTimeOffset AtualizadoEm);

/// <summary>O que a escrita devolve ao chamador: o suficiente para montar um token de consistência.</summary>
public sealed record Gravacao(Guid PedidoId, Guid ClienteId, long Versao);

/// <summary>
/// A FONTE DA VERDADE do pedido (no OrderFlow real: a tabela de Pedidos do módulo de Pedidos no SQL Server).
/// <para>
/// Cada gravação incrementa a versão do agregado e publica o evento correspondente na fila.
/// Aqui gravar e publicar acontecem sob a mesma trava — o equivalente, em memória, a gravar o evento
/// na tabela de Outbox na mesma transação (módulo 6.04). Sem Outbox, "gravou mas não publicou" é
/// exatamente a divergência que o job de reconciliação existe para encontrar.
/// </para>
/// Pronto: leia, não altere.
/// </summary>
public sealed class FonteDePedidos(FilaComAtraso<EventoDePedido> fila, TimeProvider relogio)
{
    private readonly Lock trava = new();
    private readonly Dictionary<Guid, PedidoNaFonte> pedidos = [];

    public Gravacao Criar(Guid clienteId, decimal total)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(total);
        lock (trava)
        {
            var agora = relogio.GetUtcNow();
            var pedido = new PedidoNaFonte(Guid.NewGuid(), clienteId, StatusPedido.Criado, total, 1, agora);
            pedidos[pedido.PedidoId] = pedido;
            fila.Publicar(new PedidoCriado(pedido.PedidoId, clienteId, 1, agora, total));
            return new Gravacao(pedido.PedidoId, clienteId, 1);
        }
    }

    public Gravacao AdicionarItem(Guid pedidoId, decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(valor);
        return Alterar(pedidoId,
            p => p.Status == StatusPedido.Criado ? p with { Total = p.Total + valor } : throw Invalido(p, "adicionar item"),
            (p, agora) => new ItemAdicionado(p.PedidoId, p.ClienteId, p.Versao, agora, valor));
    }

    public Gravacao Confirmar(Guid pedidoId) => Alterar(pedidoId,
        p => p.Status == StatusPedido.Criado ? p with { Status = StatusPedido.Confirmado } : throw Invalido(p, "confirmar"),
        (p, agora) => new PedidoConfirmado(p.PedidoId, p.ClienteId, p.Versao, agora));

    public Gravacao Cancelar(Guid pedidoId, string motivo) => Alterar(pedidoId,
        p => p.Status == StatusPedido.Criado ? p with { Status = StatusPedido.Cancelado } : throw Invalido(p, "cancelar"),
        (p, agora) => new PedidoCancelado(p.PedidoId, p.ClienteId, p.Versao, agora, motivo));

    public PedidoNaFonte? Obter(Guid pedidoId)
    {
        lock (trava) return pedidos.GetValueOrDefault(pedidoId);
    }

    public IReadOnlyList<PedidoNaFonte> ListarDoCliente(Guid clienteId)
    {
        lock (trava) return [.. pedidos.Values.Where(p => p.ClienteId == clienteId)];
    }

    public IReadOnlyList<PedidoNaFonte> Listar()
    {
        lock (trava) return [.. pedidos.Values];
    }

    private Gravacao Alterar(Guid pedidoId, Func<PedidoNaFonte, PedidoNaFonte> mudanca, Func<PedidoNaFonte, DateTimeOffset, EventoDePedido> evento)
    {
        lock (trava)
        {
            if (!pedidos.TryGetValue(pedidoId, out var atual))
                throw new KeyNotFoundException($"Pedido {pedidoId} não existe.");

            var agora = relogio.GetUtcNow();
            var novo = mudanca(atual) with { Versao = atual.Versao + 1, AtualizadoEm = agora };
            pedidos[pedidoId] = novo;
            fila.Publicar(evento(novo, agora));
            return new Gravacao(pedidoId, novo.ClienteId, novo.Versao);
        }
    }

    private static InvalidOperationException Invalido(PedidoNaFonte p, string acao) =>
        new($"Não é possível {acao} um pedido {p.Status}.");
}
