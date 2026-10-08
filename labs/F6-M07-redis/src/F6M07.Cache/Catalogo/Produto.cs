namespace F6M07.Cache.Catalogo;

/// <summary>Produto do catálogo do OrderFlow (o que a vitrine lê milhares de vezes por minuto).</summary>
public sealed record Produto(Guid Id, string Nome, decimal Preco, string Categoria, bool Ativo);

/// <summary>
/// A "fonte da verdade" (no OrderFlow real, o repositório EF Core sobre o SQL Server).
/// O cache nunca substitui a fonte: ele só evita ir até ela a cada leitura.
/// </summary>
public interface IFonteDeProdutos
{
    Task<Produto?> ObterAsync(Guid id, CancellationToken ct);

    Task AtualizarAsync(Produto produto, CancellationToken ct);
}
