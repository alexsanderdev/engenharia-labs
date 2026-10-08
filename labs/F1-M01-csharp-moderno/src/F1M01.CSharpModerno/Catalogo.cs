namespace F1M01.CSharpModerno;

/// <summary>
/// Catálogo simples de produtos: nullable reference types e collection expressions.
/// </summary>
public sealed class Catalogo(IEnumerable<Produto> produtosIniciais)
{
    private readonly List<Produto> _produtos = [.. produtosIniciais];

    /// <summary>Catálogo vazio.</summary>
    public Catalogo() : this([]) { }

    /// <summary>Quantidade de produtos cadastrados.</summary>
    public int Quantidade => _produtos.Count;

    /// <summary>Adiciona um produto.</summary>
    public void Adicionar(Produto produto) => _produtos.Add(produto);

    /// <summary>
    /// Busca pelo nome ignorando maiúsculas/minúsculas. Retorna <c>null</c> se não existir.
    /// </summary>
    public Produto? BuscarPorNome(string nome) =>
        throw new NotImplementedException("TODO: busque com StringComparison.OrdinalIgnoreCase e retorne null se não achar");

    /// <summary>
    /// Nome do produto encontrado ou "(produto não encontrado)". Use <c>?.</c> e <c>??</c>.
    /// </summary>
    public string NomeOuPadrao(string nome) =>
        throw new NotImplementedException("TODO: combine BuscarPorNome com ?. e ??");

    /// <summary>
    /// Retorna uma CÓPIA (snapshot) com os produtos ativos. Alterações posteriores no catálogo
    /// não afetam a lista retornada.
    /// </summary>
    public IReadOnlyList<Produto> Ativos() =>
        throw new NotImplementedException("TODO: retorne um snapshot com collection expression e spread: [.. ]");

    /// <summary>
    /// Junta duas sequências de produtos em uma nova lista, na ordem (primeiro, depois segundo),
    /// usando spread (<c>..</c>).
    /// </summary>
    public static IReadOnlyList<Produto> Mesclar(IEnumerable<Produto> primeiro, IEnumerable<Produto> segundo) =>
        throw new NotImplementedException("TODO: use [.. primeiro, .. segundo]");
}
