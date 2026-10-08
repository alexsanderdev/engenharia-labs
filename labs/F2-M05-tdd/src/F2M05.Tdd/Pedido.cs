namespace F2M05.Tdd;

/// <summary>
/// Pedido do OrderFlow com sua máquina de estados:
/// Created → Confirmed → Completed; Created → Cancelled; Completed não cancela.
/// Toda transição bem-sucedida entra no <see cref="Historico"/> com a data do <see cref="TimeProvider"/>.
/// Transição inválida lança <see cref="TransicaoInvalidaException"/> e não altera nada.
/// </summary>
public sealed class Pedido
{
    private readonly TimeProvider _relogio;
    private readonly List<TransicaoDeStatus> _historico = [];

    /// <summary>Cria um pedido no status <see cref="StatusPedido.Created"/>, com <see cref="CriadoEm"/> lido do relógio.</summary>
    public Pedido(TimeProvider relogio)
    {
        ArgumentNullException.ThrowIfNull(relogio);
        _relogio = relogio;
        CriadoEm = relogio.GetUtcNow();
    }

    /// <summary>Instante de criação (UTC), lido do <see cref="TimeProvider"/>.</summary>
    public DateTimeOffset CriadoEm { get; }

    /// <summary>Status atual. Começa em <see cref="StatusPedido.Created"/>.</summary>
    public StatusPedido Status { get; private set; } = StatusPedido.Created;

    /// <summary>Transições realizadas, em ordem. Começa vazio. Somente leitura para quem está fora.</summary>
    public IReadOnlyList<TransicaoDeStatus> Historico => _historico.AsReadOnly();

    /// <summary>Indica se, no status atual, o pedido ainda pode ser cancelado.</summary>
    public bool PodeCancelar => RegrasDeTransicao.Permitida(Status, StatusPedido.Cancelled);

    /// <summary>Created → Confirmed.</summary>
    /// <exception cref="TransicaoInvalidaException">Se o pedido não estiver em Created.</exception>
    public void Confirmar() => MudarPara(StatusPedido.Confirmed);

    /// <summary>Confirmed → Completed.</summary>
    /// <exception cref="TransicaoInvalidaException">Se o pedido não estiver em Confirmed.</exception>
    public void Concluir() => MudarPara(StatusPedido.Completed);

    /// <summary>Created → Cancelled, registrando o motivo (sem espaços nas pontas) no histórico.</summary>
    /// <exception cref="ArgumentException">Se o motivo for nulo, vazio ou só espaços.</exception>
    /// <exception cref="TransicaoInvalidaException">Se o pedido não estiver em Created.</exception>
    public void Cancelar(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        MudarPara(StatusPedido.Cancelled, motivo.Trim());
    }

    private void MudarPara(StatusPedido novo, string? motivo = null)
    {
        if (!RegrasDeTransicao.Permitida(Status, novo))
            throw new TransicaoInvalidaException(Status, novo);

        _historico.Add(new TransicaoDeStatus(Status, novo, _relogio.GetUtcNow(), motivo));
        Status = novo;
    }
}
