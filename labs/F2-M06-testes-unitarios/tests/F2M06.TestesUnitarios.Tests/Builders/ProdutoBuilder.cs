using F2M06.TestesUnitarios.Dominio;

namespace F2M06.TestesUnitarios.Tests.Builders;

/// <summary>
/// Test Data Builder de <see cref="Produto"/>: valores padrão VÁLIDOS e "sem graça", e métodos fluentes
/// para o teste dizer só o que importa para ele ("um produto inativo", "um produto de R$ 8").
/// </summary>
/// <remarks>
/// TODO (Passo 1): guarde o estado em campos privados com padrões válidos
/// (ex.: Id = Guid.NewGuid(), Nome = "Pizza Margherita", Preco = 50m, Ativo = true).
/// Cada método fluente altera um campo e devolve <c>this</c>.
/// </remarks>
public sealed class ProdutoBuilder
{
    /// <summary>Ponto de entrada fluente: <c>UmProduto().ComPreco(8m).Build()</c>.</summary>
    public static ProdutoBuilder UmProduto() => new();

    public ProdutoBuilder ComId(Guid id) =>
        throw new NotImplementedException("TODO: guarde o id e devolva this.");

    public ProdutoBuilder ComNome(string nome) =>
        throw new NotImplementedException("TODO: guarde o nome e devolva this.");

    public ProdutoBuilder ComPreco(decimal preco) =>
        throw new NotImplementedException("TODO: guarde o preço e devolva this.");

    public ProdutoBuilder Inativo() =>
        throw new NotImplementedException("TODO: marque como inativo e devolva this.");

    public Produto Build() =>
        throw new NotImplementedException("TODO: new Produto(id, nome, preco, ativo) com os valores acumulados.");

    /// <summary>Permite passar o builder onde se espera um <see cref="Produto"/>.</summary>
    public static implicit operator Produto(ProdutoBuilder builder) => builder.Build();
}
