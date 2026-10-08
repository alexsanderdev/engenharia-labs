using F2M06.TestesUnitarios.Dominio;

namespace F2M06.TestesUnitarios.Tests.Builders;

/// <summary>
/// Test Data Builder de <see cref="Produto"/>: valores padrão VÁLIDOS e "sem graça", e métodos fluentes
/// para o teste dizer só o que importa para ele ("um produto inativo", "um produto de R$ 8").
/// </summary>
public sealed class ProdutoBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _nome = "Pizza Margherita";
    private decimal _preco = 50m;
    private bool _ativo = true;

    /// <summary>Ponto de entrada fluente: <c>UmProduto().ComPreco(8m).Build()</c>.</summary>
    public static ProdutoBuilder UmProduto() => new();

    public ProdutoBuilder ComId(Guid id)
    {
        _id = id;
        return this;
    }

    public ProdutoBuilder ComNome(string nome)
    {
        _nome = nome;
        return this;
    }

    public ProdutoBuilder ComPreco(decimal preco)
    {
        _preco = preco;
        return this;
    }

    public ProdutoBuilder Inativo()
    {
        _ativo = false;
        return this;
    }

    public Produto Build() => new(_id, _nome, _preco, _ativo);

    /// <summary>Permite passar o builder onde se espera um <see cref="Produto"/>.</summary>
    public static implicit operator Produto(ProdutoBuilder builder) => builder.Build();
}
