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

    // TODO (Passo 2): você vai precisar de mais estado, por exemplo:
    //   - um buffer de eventos adiados por pedido, ordenado por versão (SortedDictionary<long, EventoDePedido>);
    //   - uma lista de esperas (pedido, versão, TaskCompletionSource) para o AguardarVersaoAsync.

    /// <summary>Total de eventos parados no buffer esperando lacuna. Ótima métrica para alertar.</summary>
    public int EventosAdiados =>
        throw new NotImplementedException("TODO: Passo 2 — conte os eventos guardados no buffer de adiados.");

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
        // TODO (Passo 2): sob a trava —
        //   1. atual = VersaoSemTrava(evento.PedidoId); versão <= atual → Ignorado.
        //   2. versão > atual + 1 → guarda no buffer (duplicata de adiado não substitui) → Adiado.
        //   3. senão: aplica (PedidoCriado cria a linha; ItemAdicionado SOMA ao total; Confirmado/Cancelado mudam o status;
        //      sempre atualizando Versao), drena os adiados contíguos e sinaliza as esperas → Aplicado.
        throw new NotImplementedException("TODO: Passo 2 — implemente Aplicar com versão por agregado (ignorar, adiar, aplicar e drenar).");
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
        // TODO (Passo 3): TaskCompletionSource com TaskCreationOptions.RunContinuationsAsynchronously
        // (você vai completá-lo DENTRO da trava). ct.Register(() => tcs.TrySetCanceled(ct)).
        throw new NotImplementedException("TODO: Passo 3 — registre a espera sob a trava e sinalize-a quando a versão for aplicada.");
    }

    /// <summary>
    /// Sobrescreve o pedido com o estado vindo da fonte da verdade (usado pela reconciliação).
    /// Nunca anda para trás: se a projeção já está numa versão maior ou igual, não faz nada.
    /// Descarta adiados com versão já coberta e aplica os que ficarem contíguos.
    /// </summary>
    public void Corrigir(PedidoResumido estadoDaFonte)
    {
        throw new NotImplementedException("TODO: Passo 4 — sobrescreva com o estado da fonte sem andar para trás, limpe/drene os adiados e sinalize as esperas.");
    }

    /// <summary>Remove um pedido da projeção (e seus adiados). Devolve <c>false</c> se ele não existia.</summary>
    public bool Remover(Guid pedidoId)
    {
        lock (trava)
        {
            // TODO (Passo 2): quando criar o buffer de adiados, remova também os adiados deste pedido.
            return pedidos.Remove(pedidoId);
        }
    }

    /// <summary>
    /// REBUILD: apaga o read model e reaplica a história inteira. Como <see cref="Aplicar"/> já trata
    /// duplicata e ordem, a história pode vir duplicada e embaralhada que o resultado é o mesmo.
    /// </summary>
    public void Reconstruir(IEnumerable<EventoDePedido> historico)
    {
        throw new NotImplementedException("TODO: Passo 2 — limpe pedidos e adiados e reaplique cada evento do histórico.");
    }

    private long VersaoSemTrava(Guid pedidoId) => pedidos.TryGetValue(pedidoId, out var p) ? p.Versao : 0;
}
