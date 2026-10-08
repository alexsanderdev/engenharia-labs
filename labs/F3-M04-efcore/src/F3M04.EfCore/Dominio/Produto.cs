namespace F3M04.EfCore.Dominio;

/// <summary>Produto do catálogo. Produto inativo não pode entrar em pedido novo.</summary>
public sealed class Produto
{
    /// <summary>Construtor usado pelo EF Core na materialização.</summary>
    private Produto() { }

    public Produto(Guid id, string sku, string nome, decimal preco, bool ativo = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentOutOfRangeException.ThrowIfNegative(preco);
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
        ArgumentOutOfRangeException.ThrowIfNegative(novoPreco);
        Preco = novoPreco;
    }

    public void Inativar() => Ativo = false;
}
