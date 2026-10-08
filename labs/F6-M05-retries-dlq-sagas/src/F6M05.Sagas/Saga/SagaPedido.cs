namespace F6M05.Sagas.Saga;

/// <summary>
/// Estado persistido da saga de pedido e a MÁQUINA DE ESTADOS dela (process manager).
/// </summary>
/// <remarks>
/// <code>
/// PedidoCriado ─► AguardandoEstoque ──EstoqueReservado──► AguardandoPagamento ──PagamentoAutorizado──► Concluida
///                       │                                     │  │
///                       └─EstoqueIndisponivel─► Cancelada     │  └─PagamentoRecusado / PrazoExpirado─► Compensando ──EstoqueLiberado──► Cancelada
/// </code>
/// A classe é pura (sem I/O): recebe a mensagem e o "agora", muda o próprio estado e devolve os
/// comandos a publicar. Quem carrega, salva e publica é o <see cref="OrquestradorSagaPedido"/>.
/// </remarks>
public sealed class SagaPedido
{
    private readonly HashSet<string> _mensagensProcessadas;

    private SagaPedido(Guid pedidoId, Guid clienteId, decimal valor, IReadOnlyList<ItemDoPedido> itens,
        DateTimeOffset criadaEm, IEnumerable<string> mensagensProcessadas)
    {
        PedidoId = pedidoId;
        ClienteId = clienteId;
        Valor = valor;
        Itens = itens;
        CriadaEm = criadaEm;
        AtualizadaEm = criadaEm;
        _mensagensProcessadas = new HashSet<string>(mensagensProcessadas, StringComparer.Ordinal);
    }

    public Guid PedidoId { get; }
    public Guid ClienteId { get; }
    public decimal Valor { get; }
    public IReadOnlyList<ItemDoPedido> Itens { get; }
    public StatusSaga Status { get; private set; }
    public PassosDaSaga Passos { get; private set; }
    public string? AutorizacaoId { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    /// <summary>Até quando o Pagamento pode responder (só enquanto <see cref="StatusSaga.AguardandoPagamento"/>).</summary>
    public DateTimeOffset? PrazoPagamentoEm { get; private set; }

    public DateTimeOffset CriadaEm { get; }
    public DateTimeOffset AtualizadaEm { get; private set; }

    /// <summary>
    /// Versão para concorrência otimista: o repositório só grava se a versão no banco ainda for
    /// esta, e então a incrementa. 0 = ainda não persistida.
    /// </summary>
    public int Versao { get; internal set; }

    /// <summary>MessageIds que já mudaram o estado desta saga (idempotência por mensagem).</summary>
    public IReadOnlyCollection<string> MensagensProcessadas => _mensagensProcessadas;

    /// <summary>A saga terminou (concluída ou cancelada)?</summary>
    public bool Terminada => Status is StatusSaga.Concluida or StatusSaga.Cancelada;

    public bool JaProcessou(string messageId) => _mensagensProcessadas.Contains(messageId);

    /// <summary>PRONTO. Recria a saga a partir do banco (usado pelo repositório).</summary>
    public static SagaPedido Reconstituir(
        Guid pedidoId, Guid clienteId, decimal valor, IReadOnlyList<ItemDoPedido> itens,
        StatusSaga status, PassosDaSaga passos, string? autorizacaoId, string? motivoCancelamento,
        DateTimeOffset? prazoPagamentoEm, DateTimeOffset criadaEm, DateTimeOffset atualizadaEm,
        int versao, IEnumerable<string> mensagensProcessadas) =>
        new(pedidoId, clienteId, valor, itens, criadaEm, mensagensProcessadas)
        {
            Status = status,
            Passos = passos,
            AutorizacaoId = autorizacaoId,
            MotivoCancelamento = motivoCancelamento,
            PrazoPagamentoEm = prazoPagamentoEm,
            AtualizadaEm = atualizadaEm,
            Versao = versao,
        };

    /// <summary>
    /// Começa a saga: status <see cref="StatusSaga.AguardandoEstoque"/>, versão 0, o MessageId do
    /// evento registrado como processado, e o comando <see cref="ReservarEstoque"/> (id determinístico).
    /// </summary>
    public static (SagaPedido Saga, IReadOnlyList<ComandoSaga> Comandos) Iniciar(PedidoCriado evento, DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(evento);
        var saga = new SagaPedido(evento.PedidoId, evento.ClienteId, evento.Valor, evento.Itens, agora, [evento.MessageId])
        {
            Status = StatusSaga.AguardandoEstoque,
        };
        return (saga, [saga.ComandoReservarEstoque()]);
    }

    /// <summary>
    /// Aplica uma mensagem ao estado atual. Devolve os comandos a publicar, ou <c>null</c> se a
    /// mensagem não faz sentido no estado atual (atrasada, repetida com outro id, fora de ordem):
    /// nesse caso NADA muda. Quando aplica, registra o MessageId e atualiza <see cref="AtualizadaEm"/>.
    /// </summary>
    /// <remarks>
    /// Transições (estado × mensagem → novo estado, passos, comandos):
    /// <list type="bullet">
    /// <item>AguardandoEstoque × EstoqueReservado → AguardandoPagamento, +EstoqueReservado, prazo = agora + PrazoDoPagamento → [AutorizarPagamento]</item>
    /// <item>AguardandoEstoque × EstoqueIndisponivel → Cancelada, +PedidoCancelado, motivo → [CancelarPedido]</item>
    /// <item>AguardandoPagamento × PagamentoAutorizado → Concluida, +PagamentoAutorizado +PedidoConfirmado, AutorizacaoId, sem prazo → [ConfirmarPedido]</item>
    /// <item>AguardandoPagamento × PagamentoRecusado → Compensando, motivo, sem prazo → [LiberarEstoque]</item>
    /// <item>AguardandoPagamento × PrazoDoPagamentoExpirado (só se agora ≥ prazo) → Compensando, motivo de prazo, sem prazo → [LiberarEstoque]</item>
    /// <item>Compensando × EstoqueLiberado → Cancelada, +EstoqueLiberado +PedidoCancelado → [CancelarPedido]</item>
    /// <item>Compensando/Cancelada × PagamentoAutorizado (resposta tardia, ainda não estornada) → mesmo status,
    /// +PagamentoAutorizado +PagamentoEstornado, AutorizacaoId → [EstornarPagamento]</item>
    /// <item>Qualquer outra combinação → <c>null</c>.</item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<ComandoSaga>? Aplicar(MensagemSaga mensagem, DateTimeOffset agora, OpcoesDaSaga opcoes)
    {
        ArgumentNullException.ThrowIfNull(mensagem);
        ArgumentNullException.ThrowIfNull(opcoes);
        if (mensagem.PedidoId != PedidoId)
            throw new ArgumentException($"Mensagem do pedido {mensagem.PedidoId} aplicada à saga {PedidoId}.", nameof(mensagem));

        IReadOnlyList<ComandoSaga>? comandos = (Status, mensagem) switch
        {
            (StatusSaga.AguardandoEstoque, EstoqueReservado) => ReservouEstoque(agora, opcoes),
            (StatusSaga.AguardandoEstoque, EstoqueIndisponivel e) => Cancelar(MotivosDeCancelamento.EstoqueIndisponivel(e.Motivo)),
            (StatusSaga.AguardandoPagamento, PagamentoAutorizado e) => Concluir(e.AutorizacaoId),
            (StatusSaga.AguardandoPagamento, PagamentoRecusado e) => Compensar(MotivosDeCancelamento.PagamentoRecusado(e.Motivo)),
            (StatusSaga.AguardandoPagamento, PrazoDoPagamentoExpirado) when agora >= PrazoPagamentoEm
                => Compensar(MotivosDeCancelamento.PrazoDoPagamentoExpirado),
            (StatusSaga.Compensando, EstoqueLiberado) => LiberouEstoque(),
            (StatusSaga.Compensando or StatusSaga.Cancelada, PagamentoAutorizado e)
                when !Passos.HasFlag(PassosDaSaga.PagamentoEstornado) => Estornar(e.AutorizacaoId),
            _ => null,
        };

        if (comandos is null) return null;

        _mensagensProcessadas.Add(mensagem.MessageId);
        AtualizadaEm = agora;
        return comandos;
    }

    /// <summary>
    /// Os comandos que correspondem ao estado atual, com os MESMOS MessageIds já emitidos. Usado
    /// quando chega uma mensagem duplicada: se a primeira execução gravou o estado mas caiu antes
    /// de publicar, republicar aqui recupera o fluxo, e o participante deduplica pelo id.
    /// </summary>
    /// <remarks>
    /// AguardandoEstoque → [ReservarEstoque]; AguardandoPagamento → [AutorizarPagamento];
    /// Compensando → [LiberarEstoque] (+ EstornarPagamento se estornado); Concluida → [ConfirmarPedido];
    /// Cancelada → [CancelarPedido] (+ EstornarPagamento se estornado).
    /// </remarks>
    public IReadOnlyList<ComandoSaga> ComandosDoEstadoAtual()
    {
        List<ComandoSaga> comandos = Status switch
        {
            StatusSaga.AguardandoEstoque => [ComandoReservarEstoque()],
            StatusSaga.AguardandoPagamento => [ComandoAutorizarPagamento()],
            StatusSaga.Compensando => [ComandoLiberarEstoque()],
            StatusSaga.Concluida => [ComandoConfirmarPedido()],
            StatusSaga.Cancelada => [ComandoCancelarPedido()],
            _ => [],
        };

        if (Passos.HasFlag(PassosDaSaga.PagamentoEstornado) && AutorizacaoId is not null)
            comandos.Add(ComandoEstornarPagamento());

        return comandos;
    }

    private IReadOnlyList<ComandoSaga> ReservouEstoque(DateTimeOffset agora, OpcoesDaSaga opcoes)
    {
        Status = StatusSaga.AguardandoPagamento;
        Passos |= PassosDaSaga.EstoqueReservado;
        PrazoPagamentoEm = agora + opcoes.PrazoDoPagamento;
        return [ComandoAutorizarPagamento()];
    }

    private IReadOnlyList<ComandoSaga> Concluir(string autorizacaoId)
    {
        Status = StatusSaga.Concluida;
        Passos |= PassosDaSaga.PagamentoAutorizado | PassosDaSaga.PedidoConfirmado;
        AutorizacaoId = autorizacaoId;
        PrazoPagamentoEm = null;
        return [ComandoConfirmarPedido()];
    }

    private IReadOnlyList<ComandoSaga> Compensar(string motivo)
    {
        Status = StatusSaga.Compensando;
        MotivoCancelamento = motivo;
        PrazoPagamentoEm = null;
        return [ComandoLiberarEstoque()];
    }

    private IReadOnlyList<ComandoSaga> LiberouEstoque()
    {
        Status = StatusSaga.Cancelada;
        Passos |= PassosDaSaga.EstoqueLiberado | PassosDaSaga.PedidoCancelado;
        return [ComandoCancelarPedido()];
    }

    private IReadOnlyList<ComandoSaga> Cancelar(string motivo)
    {
        Status = StatusSaga.Cancelada;
        Passos |= PassosDaSaga.PedidoCancelado;
        MotivoCancelamento = motivo;
        return [ComandoCancelarPedido()];
    }

    private IReadOnlyList<ComandoSaga> Estornar(string autorizacaoId)
    {
        AutorizacaoId = autorizacaoId;
        Passos |= PassosDaSaga.PagamentoAutorizado | PassosDaSaga.PagamentoEstornado;
        return [ComandoEstornarPagamento()];
    }

    // Fábricas de comandos: PRONTAS. O MessageId é determinístico (pedido + tipo do comando).
    private ReservarEstoque ComandoReservarEstoque() => new(ComandoSaga.IdPara<ReservarEstoque>(PedidoId), PedidoId, Itens);
    private AutorizarPagamento ComandoAutorizarPagamento() => new(ComandoSaga.IdPara<AutorizarPagamento>(PedidoId), PedidoId, ClienteId, Valor);
    private LiberarEstoque ComandoLiberarEstoque() => new(ComandoSaga.IdPara<LiberarEstoque>(PedidoId), PedidoId);
    private ConfirmarPedido ComandoConfirmarPedido() => new(ComandoSaga.IdPara<ConfirmarPedido>(PedidoId), PedidoId);
    private CancelarPedido ComandoCancelarPedido() => new(ComandoSaga.IdPara<CancelarPedido>(PedidoId), PedidoId, MotivoCancelamento ?? "");
    private EstornarPagamento ComandoEstornarPagamento() => new(ComandoSaga.IdPara<EstornarPagamento>(PedidoId), PedidoId, AutorizacaoId ?? "");
}
