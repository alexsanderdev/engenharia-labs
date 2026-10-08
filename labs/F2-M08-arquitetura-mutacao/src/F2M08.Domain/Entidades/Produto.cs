namespace F2M08.Domain.Entidades;

/// <summary>Produto do catálogo.</summary>
/// <remarks>
/// TODO (Passo 3): setters públicos deixam qualquer camada fazer <c>produto.Ativo = true</c> ou
/// <c>produto.Preco = -10</c> sem passar por regra nenhuma. Troque por <c>private set</c> e
/// exponha métodos com intenção: <c>AlterarPreco(decimal)</c>, <c>Desativar()</c> e <c>Ativar()</c>.
/// </remarks>
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

    public Guid Id { get; set; }
    public string Nome { get; set; }
    public decimal Preco { get; set; }
    public bool Ativo { get; set; }

    public void Desativar() => Ativo = false;
}
