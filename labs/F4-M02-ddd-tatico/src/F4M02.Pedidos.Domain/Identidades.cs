namespace F4M02.Pedidos.Domain;

/// <summary>
/// Identidade fortemente tipada do cliente. Com ids tipados, <c>Criar(produtoId, clienteId)</c> com os
/// argumentos trocados <b>não compila</b> — com dois <see cref="Guid"/> soltos, compilaria e quebraria em produção.
/// </summary>
/// <remarks>PRONTO — use como modelo para <see cref="PedidoId"/>.</remarks>
public readonly record struct ClienteId
{
    public ClienteId(Guid valor)
    {
        if (valor == Guid.Empty)
            throw new ArgumentException("ClienteId não pode ser vazio.", nameof(valor));
        Valor = valor;
    }

    public Guid Valor { get; }

    /// <summary>Gera um id novo, ordenável por tempo (UUID v7: bom para índice clusterizado).</summary>
    public static ClienteId Novo() => new(Guid.CreateVersion7());

    public override string ToString() => Valor.ToString();
}

/// <summary>Identidade fortemente tipada do produto (vem do contexto de Catálogo).</summary>
/// <remarks>PRONTO.</remarks>
public readonly record struct ProdutoId
{
    public ProdutoId(Guid valor)
    {
        if (valor == Guid.Empty)
            throw new ArgumentException("ProdutoId não pode ser vazio.", nameof(valor));
        Valor = valor;
    }

    public Guid Valor { get; }

    public static ProdutoId Novo() => new(Guid.CreateVersion7());

    public override string ToString() => Valor.ToString();
}
