namespace F4M01.Domain.Produtos;

/// <summary>Produto do catálogo. O preço oficial é SEMPRE o daqui, nunca o que o cliente manda.</summary>
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

    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public decimal Preco { get; private set; }
    public bool Ativo { get; private set; }

    public void Desativar() => Ativo = false;
}
