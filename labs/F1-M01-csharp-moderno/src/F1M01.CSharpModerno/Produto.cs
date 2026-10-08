namespace F1M01.CSharpModerno;

/// <summary>
/// Produto do catálogo. Record imutável com propriedades <c>required</c>/<c>init</c>.
/// Para "alterar", crie uma cópia com <c>with</c>.
/// </summary>
public sealed record Produto
{
    /// <summary>Identificador. Gerado automaticamente se não informado.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Nome obrigatório. Nome nulo ou em branco lança <see cref="ArgumentException"/>,
    /// inclusive quando atribuído via <c>with</c>. O nome é guardado sem espaços nas pontas.
    /// </summary>
    public required string Nome
    {
        get;
        init
        {
            // TODO: valide (nulo/em branco lança ArgumentException) e guarde o nome sem espaços nas pontas.
            // A palavra-chave `field` (C# 14) é o backing field gerado pelo compilador.
            field = value;
        }
    }

    /// <summary>Preço obrigatório.</summary>
    public required Dinheiro Preco { get; init; }

    /// <summary>Descrição opcional: <c>null</c> significa "sem descrição".</summary>
    public string? Descricao { get; init; }

    /// <summary>Produto inativo não entra em pedido novo.</summary>
    public bool Ativo { get; init; } = true;

    /// <summary>Retorna uma cópia com o novo preço; o original não muda.</summary>
    public Produto ComPreco(Dinheiro novoPreco) =>
        throw new NotImplementedException("TODO: retorne uma cópia com o novo preço usando with");

    /// <summary>Retorna uma cópia inativa; o original não muda.</summary>
    public Produto Desativar() =>
        throw new NotImplementedException("TODO: retorne uma cópia inativa usando with");

    /// <summary>
    /// "Nome — Descrição" quando há descrição não vazia; senão, apenas "Nome".
    /// </summary>
    public string Resumo() =>
        throw new NotImplementedException("TODO: use um property pattern (Descricao is { Length: > 0 }) para montar o resumo");
}
