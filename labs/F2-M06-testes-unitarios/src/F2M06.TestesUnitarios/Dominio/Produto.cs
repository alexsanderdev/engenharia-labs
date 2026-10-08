namespace F2M06.TestesUnitarios.Dominio;

/// <summary>Produto do catálogo. Só produtos ativos entram em pedido novo.</summary>
public sealed class Produto
{
    public Produto(Guid id, string nome, decimal preco, bool ativo = true)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id do produto é obrigatório.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(nome);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(preco);

        Id = id;
        Nome = nome.Trim();
        Preco = preco;
        Ativo = ativo;
    }

    public Guid Id { get; }
    public string Nome { get; }
    public decimal Preco { get; }
    public bool Ativo { get; private set; }

    public void Desativar() => Ativo = false;
}
