namespace F6M04.Pedidos.Dominio;

/// <summary>Ciclo de vida resumido do pedido no OrderFlow.</summary>
public enum StatusPedido
{
    Criado = 1,
    Confirmado = 2,
    Cancelado = 3,
}

/// <summary>
/// PRONTO. Agregado Pedido (versão enxuta para o lab). Cada mudança de estado relevante para fora
/// registra um evento em <see cref="Eventos"/>; nada é publicado daqui.
/// </summary>
public sealed class Pedido : ITemEventos
{
    private readonly List<IEventoDeIntegracao> _eventos = [];

    private Pedido() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Número legível e ÚNICO (ex.: "PED-0001"). A constraint única ajuda os testes a provocar falha no commit.</summary>
    public string Numero { get; private set; } = "";

    public string ClienteEmail { get; private set; } = "";

    public decimal Total { get; private set; }

    public StatusPedido Status { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }

    public DateTimeOffset? ConfirmadoEm { get; private set; }

    /// <summary>Sobe 1 a cada evento (token de concorrência no EF Core). Ver <see cref="ITemEventos.Versao"/>.</summary>
    public int Versao { get; private set; }

    public IReadOnlyList<IEventoDeIntegracao> Eventos => _eventos;

    public void LimparEventos() => _eventos.Clear();

    public static Pedido Criar(string numero, string clienteEmail, decimal total, DateTimeOffset agora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(numero);
        ArgumentException.ThrowIfNullOrWhiteSpace(clienteEmail);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(total);

        var pedido = new Pedido
        {
            Id = Guid.CreateVersion7(agora),
            Numero = numero,
            ClienteEmail = clienteEmail,
            Total = total,
            Status = StatusPedido.Criado,
            CriadoEm = agora,
        };
        pedido.Registrar(new PedidoCriado(pedido.Id, numero, clienteEmail, total, agora));
        return pedido;
    }

    public void Confirmar(DateTimeOffset agora)
    {
        if (Status != StatusPedido.Criado)
            throw new InvalidOperationException($"Só pedidos criados podem ser confirmados (status atual: {Status}).");

        Status = StatusPedido.Confirmado;
        ConfirmadoEm = agora;
        Registrar(new PedidoConfirmado(Id, Numero, ClienteEmail, agora));
    }

    private void Registrar(IEventoDeIntegracao evento)
    {
        Versao++;
        _eventos.Add(evento);
    }
}
