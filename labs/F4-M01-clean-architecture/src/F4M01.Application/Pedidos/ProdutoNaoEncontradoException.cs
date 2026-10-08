namespace F4M01.Application.Pedidos;

/// <summary>O comando citou um produto que não existe no catálogo (a Api traduz para 404).</summary>
public sealed class ProdutoNaoEncontradoException(Guid produtoId)
    : Exception($"Produto não encontrado: {produtoId}")
{
    public Guid ProdutoId { get; } = produtoId;
}
