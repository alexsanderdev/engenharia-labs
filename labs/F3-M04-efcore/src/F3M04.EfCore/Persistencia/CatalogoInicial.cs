using F3M04.EfCore.Dominio;

namespace F3M04.EfCore.Persistencia;

/// <summary>
/// Dados de referência que TODO ambiente precisa ter (dev, teste, produção): o catálogo inicial.
/// Ids fixos são obrigatórios no <c>HasData</c>: o EF Core compara o seed entre migrations
/// pela chave para decidir se gera INSERT, UPDATE ou DELETE.
/// </summary>
public static class CatalogoInicial
{
    public static readonly Guid CanetaId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");
    public static readonly Guid CadernoId = Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7");
    public static readonly Guid MochilaId = Guid.Parse("c9bf9e57-1685-4c89-bafb-ff5af830be8a");

    /// <summary>Os três produtos do catálogo inicial (todos ativos).</summary>
    public static IReadOnlyList<Produto> Produtos { get; } =
    [
        new Produto(CanetaId, "SEED-CANETA", "Caneta azul", 3.50m),
        new Produto(CadernoId, "SEED-CADERNO", "Caderno 96 folhas", 24.90m),
        new Produto(MochilaId, "SEED-MOCHILA", "Mochila executiva", 189.00m),
    ];
}
