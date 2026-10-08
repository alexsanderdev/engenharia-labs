namespace F2M03.CodeSmells.FeatureEnvy;

/// <summary>Item de um pedido: preço e peso são por unidade.</summary>
public sealed class ItemDoPedido(string sku, decimal precoUnitario, int quantidade, decimal pesoUnitarioKg)
{
    public string Sku { get; } = sku;
    public decimal PrecoUnitario { get; } = precoUnitario;
    public int Quantidade { get; } = quantidade;
    public decimal PesoUnitarioKg { get; } = pesoUnitarioKg;

    /// <summary>Preço unitário × quantidade.</summary>
    public decimal Subtotal() => throw new NotImplementedException("TODO: preço unitário × quantidade.");

    /// <summary>Peso unitário × quantidade.</summary>
    public decimal PesoTotalKg() => throw new NotImplementedException("TODO: peso unitário × quantidade.");
}

/// <summary>Endereço de entrega (só o que o frete precisa).</summary>
public sealed class EnderecoDeEntrega(string uf)
{
    public string Uf { get; } = uf;

    /// <summary>
    /// Taxa base do frete pela região da UF (sem diferenciar maiúsculas):
    /// Sudeste R$ 15, Sul R$ 20, demais R$ 35.
    /// </summary>
    public decimal TaxaBaseDeFrete() =>
        throw new NotImplementedException("TODO: mova para cá a decisão de região que hoje está em CalculadoraDeFrete.");
}

/// <summary>Pedido do OrderFlow, reduzido ao que o cálculo de frete usa.</summary>
public sealed class Pedido(EnderecoDeEntrega entrega, IReadOnlyList<ItemDoPedido> itens)
{
    /// <summary>Acima deste subtotal o frete é grátis.</summary>
    public const decimal LimiteFreteGratis = 300m;

    public EnderecoDeEntrega Entrega { get; } = entrega;
    public IReadOnlyList<ItemDoPedido> Itens { get; } = itens;

    /// <summary>Soma dos subtotais dos itens.</summary>
    public decimal Subtotal() => throw new NotImplementedException("TODO: some ItemDoPedido.Subtotal() de cada item.");

    /// <summary>Soma dos pesos dos itens, em kg.</summary>
    public decimal PesoTotalKg() => throw new NotImplementedException("TODO: some ItemDoPedido.PesoTotalKg() de cada item.");

    /// <summary>Verdadeiro quando o subtotal atinge <see cref="LimiteFreteGratis"/>.</summary>
    public bool TemFreteGratis() => throw new NotImplementedException("TODO: Subtotal() >= LimiteFreteGratis.");
}
