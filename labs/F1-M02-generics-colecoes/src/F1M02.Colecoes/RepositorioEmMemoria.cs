namespace F1M02.Colecoes;

/// <summary>
/// Visão somente leitura de um repositório. Repare na variância:
/// <c>out TEntidade</c> (só sai) e <c>in TId</c> (só entra). Assim, um
/// <c>IRepositorioLeitura&lt;Produto, Guid&gt;</c> pode ser usado como
/// <c>IRepositorioLeitura&lt;IComPreco, Guid&gt;</c>.
/// </summary>
public interface IRepositorioLeitura<out TEntidade, in TId> where TId : notnull
{
    /// <summary>Quantidade de entidades armazenadas.</summary>
    int Quantidade { get; }

    /// <summary>Entidade com o id ou <c>null</c> (default) se não existir.</summary>
    TEntidade? ObterPorId(TId id);

    /// <summary>Snapshot de todas as entidades.</summary>
    IReadOnlyCollection<TEntidade> Listar();
}

/// <summary>
/// Repositório genérico em memória, baseado em <see cref="Dictionary{TKey, TValue}"/> (busca O(1) por id).
/// </summary>
/// <typeparam name="TEntidade">Tipo de referência que tem identidade.</typeparam>
/// <typeparam name="TId">Tipo do id; não pode ser nulo (exigência do Dictionary).</typeparam>
public sealed class RepositorioEmMemoria<TEntidade, TId> : IRepositorioLeitura<TEntidade, TId>
    where TEntidade : class, IEntidade<TId>
    where TId : notnull
{
    private readonly Dictionary<TId, TEntidade> _itens;

    /// <summary>
    /// Cria o repositório. Um comparador opcional permite, por exemplo, ids string sem diferenciar maiúsculas.
    /// </summary>
    public RepositorioEmMemoria(IEqualityComparer<TId>? comparador = null) =>
        _itens = new Dictionary<TId, TEntidade>(comparador);

    /// <inheritdoc />
    public int Quantidade => _itens.Count;

    /// <summary>
    /// Adiciona a entidade. Retorna <c>false</c> (sem lançar e sem sobrescrever) se o id já existir.
    /// Entidade nula lança <see cref="ArgumentNullException"/>.
    /// </summary>
    public bool Adicionar(TEntidade entidade) =>
        throw new NotImplementedException("TODO: valide nulo e use _itens.TryAdd(entidade.Id, entidade)");

    /// <summary>
    /// Substitui uma entidade existente. Id inexistente lança <see cref="KeyNotFoundException"/>.
    /// </summary>
    public void Atualizar(TEntidade entidade) =>
        throw new NotImplementedException("TODO: lance KeyNotFoundException se o id não existir; senão, substitua");

    /// <summary>Remove pelo id. Retorna <c>true</c> se removeu.</summary>
    public bool Remover(TId id) =>
        throw new NotImplementedException("TODO: remova do dicionário");

    /// <inheritdoc />
    public TEntidade? ObterPorId(TId id) =>
        throw new NotImplementedException("TODO: retorne a entidade ou null (GetValueOrDefault ajuda)");

    /// <summary>
    /// Snapshot: uma cópia. Adicionar/remover depois NÃO altera a coleção já retornada.
    /// </summary>
    public IReadOnlyCollection<TEntidade> Listar() =>
        throw new NotImplementedException("TODO: retorne uma CÓPIA dos valores (cuidado: _itens.Values é uma visão viva)");

    /// <summary>Snapshot filtrado pelo predicado.</summary>
    public IReadOnlyCollection<TEntidade> Buscar(Func<TEntidade, bool> filtro) =>
        throw new NotImplementedException("TODO: retorne uma cópia filtrada");
}
