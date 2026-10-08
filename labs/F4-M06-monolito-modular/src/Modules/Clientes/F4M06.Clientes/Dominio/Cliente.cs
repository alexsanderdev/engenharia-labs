namespace F4M06.Clientes.Dominio;

public enum NivelFidelidade
{
    Bronze,
    Prata,
    Ouro,
}

/// <summary>Cliente e o programa de fidelidade. Entidade PRIVADA do módulo Clientes.</summary>
public sealed class Cliente
{
    public const decimal LimitePrata = 1_000m;
    public const decimal LimiteOuro = 5_000m;

    private Cliente() { } // EF Core

    public Cliente(Guid id, string nome, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        Id = id;
        Nome = nome;
        Email = email;
        Nivel = NivelFidelidade.Bronze;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = "";
    public string Email { get; private set; } = "";
    public decimal TotalGasto { get; private set; }
    public int PedidosConfirmados { get; private set; }
    public NivelFidelidade Nivel { get; private set; }

    /// <summary>Contabiliza uma compra confirmada e recalcula o nível de fidelidade.</summary>
    public void RegistrarCompra(decimal valor)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(valor);
        TotalGasto += valor;
        PedidosConfirmados++;
        Nivel = TotalGasto >= LimiteOuro ? NivelFidelidade.Ouro
            : TotalGasto >= LimitePrata ? NivelFidelidade.Prata
            : NivelFidelidade.Bronze;
    }
}

/// <summary>
/// Registro de "este pedido já foi contabilizado" (deduplicação no assinante).
/// A chave é o <c>PedidoId</c>: um pedido só é confirmado uma vez, então o mesmo evento
/// entregue duas vezes não pode somar duas vezes.
/// </summary>
public sealed class PedidoContabilizado
{
    private PedidoContabilizado() { } // EF Core

    public PedidoContabilizado(Guid pedidoId, Guid clienteId, decimal valor, DateTimeOffset contabilizadoEm)
    {
        PedidoId = pedidoId;
        ClienteId = clienteId;
        Valor = valor;
        ContabilizadoEm = contabilizadoEm;
    }

    public Guid PedidoId { get; private set; }
    public Guid ClienteId { get; private set; }
    public decimal Valor { get; private set; }
    public DateTimeOffset ContabilizadoEm { get; private set; }
}
