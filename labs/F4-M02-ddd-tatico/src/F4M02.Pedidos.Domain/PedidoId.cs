namespace F4M02.Pedidos.Domain;

/// <summary>
/// Identidade fortemente tipada do pedido. Igualdade por valor (é um record struct),
/// nunca vazia, gerada pelo próprio domínio com <see cref="Novo"/>.
/// </summary>
public readonly record struct PedidoId
{
    /// <summary>Cria um id a partir de um <see cref="Guid"/> existente (ex.: ao reidratar do banco).</summary>
    /// <exception cref="ArgumentException">Se <paramref name="valor"/> for <see cref="Guid.Empty"/>.</exception>
    public PedidoId(Guid valor)
    {
        if (valor == Guid.Empty)
            throw new ArgumentException("PedidoId não pode ser vazio.", nameof(valor));
        Valor = valor;
    }

    public Guid Valor { get; }

    /// <summary>Gera um id novo e único (UUID v7, ordenável por tempo).</summary>
    public static PedidoId Novo() => new(Guid.CreateVersion7());

    public override string ToString() => Valor.ToString();
}
