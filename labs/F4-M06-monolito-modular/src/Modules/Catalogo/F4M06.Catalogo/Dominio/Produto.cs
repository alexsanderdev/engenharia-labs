namespace F4M06.Catalogo.Dominio;

/// <summary>Produto do catálogo. Entidade PRIVADA do módulo Catálogo.</summary>
internal sealed class Produto
{
    private Produto() { } // EF Core

    public Produto(Guid id, string nome, decimal preco)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(preco);
        Id = id;
        Nome = nome;
        Preco = preco;
        Ativo = true;
    }

    public Guid Id { get; private set; }
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
