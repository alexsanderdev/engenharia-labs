namespace F4M02.Pedidos.Domain;

/// <summary>
/// Identidade fortemente tipada do pedido. Igualdade por valor (é um record struct),
/// nunca vazia, gerada pelo próprio domínio com <see cref="Novo"/>.
/// </summary>
/// <remarks>Use <see cref="ClienteId"/> (em Identidades.cs) como modelo.</remarks>
public readonly record struct PedidoId
{
    /// <summary>Cria um id a partir de um <see cref="Guid"/> existente (ex.: ao reidratar do banco).</summary>
    /// <exception cref="ArgumentException">Se <paramref name="valor"/> for <see cref="Guid.Empty"/>.</exception>
    public PedidoId(Guid valor) =>
        throw new NotImplementedException("TODO (Passo 1): rejeite Guid.Empty com ArgumentException e guarde o valor em Valor.");

    public Guid Valor { get; }

    /// <summary>Gera um id novo e único (UUID v7, ordenável por tempo).</summary>
    public static PedidoId Novo() =>
        throw new NotImplementedException("TODO (Passo 1): new(Guid.CreateVersion7()).");

    public override string ToString() =>
        throw new NotImplementedException("TODO (Passo 1): devolva o Guid como texto.");
}
