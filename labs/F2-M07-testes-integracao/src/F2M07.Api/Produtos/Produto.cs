namespace F2M07.Api.Produtos;

/// <summary>Produto do catálogo. O SKU é único no banco (índice único).</summary>
public sealed class Produto
{
    // Construtor usado pelo EF Core na materialização.
    private Produto() { }

    public Produto(Guid id, string sku, string nome, decimal preco, bool ativo = true)
    {
        Id = id;
        Sku = sku;
        Nome = nome;
        Preco = preco;
        Ativo = ativo;
    }

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = "";
    public string Nome { get; private set; } = "";
    public decimal Preco { get; private set; }
    public bool Ativo { get; private set; }

    public void AlterarPreco(decimal novoPreco)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(novoPreco);
        Preco = novoPreco;
    }

    public void Desativar() => Ativo = false;
}

/// <summary>Entrada de POST /produtos.</summary>
public sealed record ProdutoRequest(string? Sku, string? Nome, decimal Preco);

/// <summary>Saída dos endpoints de produto (nunca devolvemos a entidade).</summary>
public sealed record ProdutoResponse(Guid Id, string Sku, string Nome, decimal Preco, bool Ativo)
{
    public static ProdutoResponse De(Produto p) => new(p.Id, p.Sku, p.Nome, p.Preco, p.Ativo);
}
