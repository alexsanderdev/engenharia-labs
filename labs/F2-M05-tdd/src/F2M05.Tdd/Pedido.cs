namespace F2M05.Tdd;

/// <summary>
/// Pedido do OrderFlow com sua máquina de estados:
/// Created → Confirmed → Completed; Created → Cancelled; Completed não cancela.
/// Toda transição bem-sucedida entra no <see cref="Historico"/> com a data do <see cref="TimeProvider"/>.
/// Transição inválida lança <see cref="TransicaoInvalidaException"/> e não altera nada.
/// </summary>
/// <remarks>
/// NÃO implemente tudo de uma vez. Construa esta classe com TDD, um teste por vez,
/// seguindo o "Roteiro de ciclos" da nota Lab. Escreva seus testes em tests/F2M05.Tdd.Tests/MeusTestes/.
/// Você pode (e deve) mudar o CORPO de tudo aqui e criar classes novas no refactor;
/// só mantenha as assinaturas públicas, que os testes de aceitação usam.
/// </remarks>
public sealed class Pedido
{
    private readonly TimeProvider _relogio;

    /// <summary>Cria um pedido no status <see cref="StatusPedido.Created"/>, com <see cref="CriadoEm"/> lido do relógio.</summary>
    public Pedido(TimeProvider relogio)
    {
        ArgumentNullException.ThrowIfNull(relogio);
        _relogio = relogio;
    }

    /// <summary>Instante de criação (UTC), lido do <see cref="TimeProvider"/>.</summary>
    public DateTimeOffset CriadoEm => throw new NotImplementedException("TODO (TDD): guarde _relogio.GetUtcNow() no construtor. Triangule com duas datas.");

    /// <summary>Status atual. Começa em <see cref="StatusPedido.Created"/>.</summary>
    public StatusPedido Status => throw new NotImplementedException("TODO (TDD): ciclo 1 — comece com 'fake it' (StatusPedido.Created).");

    /// <summary>Transições realizadas, em ordem. Começa vazio. Somente leitura para quem está fora.</summary>
    public IReadOnlyList<TransicaoDeStatus> Historico => throw new NotImplementedException("TODO (TDD): lista privada exposta como somente leitura.");

    /// <summary>Indica se, no status atual, o pedido ainda pode ser cancelado.</summary>
    public bool PodeCancelar => throw new NotImplementedException("TODO (TDD): reaproveite a regra de transição (não duplique o if).");

    /// <summary>Created → Confirmed.</summary>
    /// <exception cref="TransicaoInvalidaException">Se o pedido não estiver em Created.</exception>
    public void Confirmar() => throw new NotImplementedException("TODO (TDD): mude o status e registre a transição com a data do relógio.");

    /// <summary>Confirmed → Completed.</summary>
    /// <exception cref="TransicaoInvalidaException">Se o pedido não estiver em Confirmed.</exception>
    public void Concluir() => throw new NotImplementedException("TODO (TDD): só a partir de Confirmed.");

    /// <summary>Created → Cancelled, registrando o motivo (sem espaços nas pontas) no histórico.</summary>
    /// <exception cref="ArgumentException">Se o motivo for nulo, vazio ou só espaços.</exception>
    /// <exception cref="TransicaoInvalidaException">Se o pedido não estiver em Created.</exception>
    public void Cancelar(string motivo) => throw new NotImplementedException("TODO (TDD): motivo obrigatório; só a partir de Created; Completed não cancela.");
}
