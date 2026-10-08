using F6M08.Consistencia.Dominio;

namespace F6M08.Consistencia.Projecao;

/// <summary>
/// Projeção (read model) "resumo de pedidos do cliente", alimentada por eventos que podem chegar
/// atrasados, duplicados e fora de ordem.
/// <para>
/// Regra central: para cada pedido, a projeção guarda a ÚLTIMA VERSÃO APLICADA. Um evento só é aplicado
/// se for exatamente a próxima versão (<c>atual + 1</c>). Versão menor ou igual → já visto (ignora).
/// Versão maior → há lacuna: guarda no buffer e aplica quando a lacuna for preenchida.
/// </para>
/// <para>
/// É acessada ao mesmo tempo pelo consumidor (escrita) e pelas requisições (leitura e espera):
/// todo estado fica sob uma única trava.
/// </para>
/// </summary>
public sealed class ProjecaoResumoDoCliente
{
    private readonly Lock trava = new();
    private readonly Dictionary<Guid, PedidoResumido> pedidos = [];
    private readonly Dictionary<Guid, SortedDictionary<long, EventoDePedido>> adiados = [];
    private readonly List<Espera> esperas = [];

    private sealed record Espera(Guid PedidoId, long Versao, TaskCompletionSource Sinal);

    /// <summary>Total de eventos parados no buffer esperando lacuna. Ótima métrica para alertar.</summary>
    public int EventosAdiados
    {
        get { lock (trava) return adiados.Values.Sum(b => b.Count); }
    }

    /// <summary>
    /// Aplica um evento respeitando a versão do agregado.
    /// </summary>
    /// <returns>
    /// <see cref="ResultadoDaAplicacao.Aplicado"/> se <c>evento.Versao == atual + 1</c> (e, em seguida, aplica os
    /// adiados que ficaram contíguos); <see cref="ResultadoDaAplicacao.Ignorado"/> se <c>evento.Versao &lt;= atual</c>;
    /// <see cref="ResultadoDaAplicacao.Adiado"/> se <c>evento.Versao &gt; atual + 1</c>.
    /// </returns>
    public ResultadoDaAplicacao Aplicar(EventoDePedido evento)
    {
        ArgumentNullException.ThrowIfNull(evento);
        lock (trava)
        {
            var atual = VersaoSemTrava(evento.PedidoId);
            if (evento.Versao <= atual) return ResultadoDaAplicacao.Ignorado;

            if (evento.Versao > atual + 1)
            {
                if (!adiados.TryGetValue(evento.PedidoId, out var buffer))
                    adiados[evento.PedidoId] = buffer = [];
                buffer.TryAdd(evento.Versao, evento); // duplicata de um adiado: fica a primeira
                return ResultadoDaAplicacao.Adiado;
            }

            AplicarNaOrdem(evento);
            DrenarAdiados(evento.PedidoId);
            SinalizarEsperas();
            return ResultadoDaAplicacao.Aplicado;
        }
    }

    /// <summary>Última versão aplicada do pedido (0 se a projeção ainda não conhece o pedido).</summary>
    public long VersaoDoPedido(Guid pedidoId)
    {
        lock (trava) return VersaoSemTrava(pedidoId);
    }

    public PedidoResumido? ObterPedido(Guid pedidoId)
    {
        lock (trava) return pedidos.GetValueOrDefault(pedidoId);
    }

    /// <summary>Todos os pedidos projetados (usado pela reconciliação).</summary>
    public IReadOnlyList<PedidoResumido> Todos()
    {
        lock (trava) return [.. pedidos.Values];
    }

    /// <summary>O resumo do cliente como a projeção o enxerga AGORA (pode estar atrasado). Cliente sem pedidos → lista vazia.</summary>
    public ResumoDoCliente ObterResumo(Guid clienteId)
    {
        lock (trava)
        {
            return new ResumoDoCliente(clienteId,
                [.. pedidos.Values.Where(p => p.ClienteId == clienteId).OrderBy(p => p.PedidoId)]);
        }
    }

    /// <summary>
    /// Completa quando a projeção tiver aplicado pelo menos <paramref name="versao"/> do pedido.
    /// Se já alcançou, devolve uma Task já completada. A verificação e o registro da espera acontecem
    /// sob a mesma trava (senão o evento pode ser aplicado entre "verifiquei" e "registrei" e a espera nunca acorda).
    /// Cancelável por <paramref name="ct"/>.
    /// </summary>
    public Task AguardarVersaoAsync(Guid pedidoId, long versao, CancellationToken ct = default)
    {
        lock (trava)
        {
            if (VersaoSemTrava(pedidoId) >= versao) return Task.CompletedTask;
            if (ct.IsCancellationRequested) return Task.FromCanceled(ct);

            var sinal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            esperas.Add(new Espera(pedidoId, versao, sinal));
            if (ct.CanBeCanceled)
            {
                var registro = ct.Register(() => sinal.TrySetCanceled(ct));
                sinal.Task.ContinueWith(_ => registro.Dispose(), TaskScheduler.Default);
            }
            return sinal.Task;
        }
    }

    /// <summary>
    /// Sobrescreve o pedido com o estado vindo da fonte da verdade (usado pela reconciliação).
    /// Nunca anda para trás: se a projeção já está numa versão maior ou igual, não faz nada.
    /// Descarta adiados com versão já coberta e aplica os que ficarem contíguos.
    /// </summary>
    public void Corrigir(PedidoResumido estadoDaFonte)
    {
        ArgumentNullException.ThrowIfNull(estadoDaFonte);
        lock (trava)
        {
            if (VersaoSemTrava(estadoDaFonte.PedidoId) >= estadoDaFonte.Versao) return;

            pedidos[estadoDaFonte.PedidoId] = estadoDaFonte;
            if (adiados.TryGetValue(estadoDaFonte.PedidoId, out var buffer))
            {
                foreach (var versao in buffer.Keys.Where(v => v <= estadoDaFonte.Versao).ToList())
                    buffer.Remove(versao);
                if (buffer.Count == 0) adiados.Remove(estadoDaFonte.PedidoId);
            }
            DrenarAdiados(estadoDaFonte.PedidoId);
            SinalizarEsperas();
        }
    }

    /// <summary>Remove um pedido da projeção (e seus adiados). Devolve <c>false</c> se ele não existia.</summary>
    public bool Remover(Guid pedidoId)
    {
        lock (trava)
        {
            adiados.Remove(pedidoId);
            return pedidos.Remove(pedidoId);
        }
    }

    /// <summary>
    /// REBUILD: apaga o read model e reaplica a história inteira. Como <see cref="Aplicar"/> já trata
    /// duplicata e ordem, a história pode vir duplicada e embaralhada que o resultado é o mesmo.
    /// </summary>
    public void Reconstruir(IEnumerable<EventoDePedido> historico)
    {
        ArgumentNullException.ThrowIfNull(historico);
        lock (trava)
        {
            pedidos.Clear();
            adiados.Clear();
            foreach (var evento in historico) Aplicar(evento); // Lock é reentrante
        }
    }

    private long VersaoSemTrava(Guid pedidoId) => pedidos.TryGetValue(pedidoId, out var p) ? p.Versao : 0;

    private void AplicarNaOrdem(EventoDePedido evento)
    {
        var atual = pedidos.GetValueOrDefault(evento.PedidoId);
        pedidos[evento.PedidoId] = (evento, atual) switch
        {
            (PedidoCriado c, null) => new PedidoResumido(c.PedidoId, c.ClienteId, StatusPedido.Criado, c.Total, c.Versao),
            (ItemAdicionado i, not null) => atual with { Total = atual.Total + i.Valor, Versao = i.Versao },
            (PedidoConfirmado c, not null) => atual with { Status = StatusPedido.Confirmado, Versao = c.Versao },
            (PedidoCancelado c, not null) => atual with { Status = StatusPedido.Cancelado, Versao = c.Versao },
            _ => throw new InvalidOperationException(
                $"Evento {evento.GetType().Name} v{evento.Versao} inválido para o pedido {evento.PedidoId}."),
        };
    }

    private void DrenarAdiados(Guid pedidoId)
    {
        if (!adiados.TryGetValue(pedidoId, out var buffer)) return;
        while (buffer.Count > 0)
        {
            var (versao, evento) = buffer.First();
            var atual = VersaoSemTrava(pedidoId);
            if (versao <= atual) { buffer.Remove(versao); continue; }
            if (versao != atual + 1) break;
            AplicarNaOrdem(evento);
            buffer.Remove(versao);
        }
        if (buffer.Count == 0) adiados.Remove(pedidoId);
    }

    private void SinalizarEsperas()
    {
        esperas.RemoveAll(e =>
        {
            if (e.Sinal.Task.IsCompleted) return true; // cancelada
            if (VersaoSemTrava(e.PedidoId) < e.Versao) return false;
            e.Sinal.TrySetResult();
            return true;
        });
    }
}
