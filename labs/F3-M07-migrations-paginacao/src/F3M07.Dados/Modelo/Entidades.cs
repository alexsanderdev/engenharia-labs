namespace F3M07.Dados.Modelo;

public enum StatusPedido
{
    Created,
    Confirmed,
    Completed,
    Cancelled,
}

public sealed class Cliente
{
    /// <summary>Identity (int) gerado pelo banco.</summary>
    public int Id { get; set; }
    public string Nome { get; set; } = "";
}

public sealed class Pedido
{
    /// <summary>Identity (int) gerado pelo banco. Também é o desempate da paginação keyset.</summary>
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime CriadoEm { get; set; }
    public StatusPedido Status { get; set; } = StatusPedido.Created;
    public decimal Total { get; set; }

    /// <summary>
    /// Canal de venda ("Web", "Loja", "Marketplace"...). Coluna NOVA: entra na SEGUNDA migration (Passo 3).
    /// </summary>
    public string Canal { get; set; } = "Web";

    /// <summary>
    /// Token de concorrência. Configurado como <c>rowversion</c>, o SQL Server troca o valor a cada UPDATE
    /// e o EF Core o usa no WHERE dos UPDATE/DELETE (Passo 1 e Passo 5).
    /// </summary>
    public byte[] Versao { get; set; } = [];

    /// <summary>Aplica um desconto percentual sobre o total ATUAL, arredondando em 2 casas.</summary>
    public void AplicarDesconto(decimal percentual)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(percentual);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(percentual, 100m);
        Total = Math.Round(Total * (1 - percentual / 100m), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Created/Confirmed → Cancelled. Completed não cancela.</summary>
    public void Cancelar()
    {
        if (Status == StatusPedido.Completed)
            throw new InvalidOperationException("Pedido concluído não pode ser cancelado.");
        Status = StatusPedido.Cancelled;
    }
}
