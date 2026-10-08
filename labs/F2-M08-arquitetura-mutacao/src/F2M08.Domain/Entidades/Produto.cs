namespace F2M08.Domain.Entidades;

/// <summary>Produto do catálogo. Estado só muda por métodos com intenção (nada de setter público).</summary>
public sealed class Produto
{
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
    public string Nome { get; private set; }
    public decimal Preco { get; private set; }
    public bool Ativo { get; private set; }

    public void AlterarPreco(decimal novoPreco)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(novoPreco);
        Preco = novoPreco;
    }

    public void Desativar() => Ativo = false;

    public void Ativar() => Ativo = true;
}
