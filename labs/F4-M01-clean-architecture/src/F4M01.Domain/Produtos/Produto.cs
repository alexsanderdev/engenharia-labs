namespace F4M01.Domain.Produtos;

/// <summary>Produto do catálogo. O preço oficial é SEMPRE o daqui, nunca o que o cliente manda.</summary>
/// <remarks>
/// TODO (Passo 2): setters públicos deixam qualquer camada "ativar" um produto ou trocar o preço por atribuição.
/// Troque por <c>private set</c> (o EF Core continua conseguindo materializar) e use métodos com intenção
/// (ex.: <c>Desativar()</c>).
/// </remarks>
public sealed class Produto
{
    public Produto(Guid id, string nome, decimal preco, bool ativo = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentOutOfRangeException.ThrowIfNegative(preco);
        Id = id;
        Nome = nome;
        Preco = preco;
        Ativo = ativo;
    }

    public Guid Id { get; set; }
    public string Nome { get; set; }
    public decimal Preco { get; set; }
    public bool Ativo { get; set; }

    public void Desativar() => Ativo = false;
}
