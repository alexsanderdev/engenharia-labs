using System.Collections.Concurrent;

namespace F5M04.Api.Catalogo;

/// <summary>Produto do catálogo do OrderFlow. Só administradores criam e desativam.</summary>
public sealed class Produto(Guid id, string nome, decimal preco, bool ativo = true)
{
    public Guid Id { get; } = id;
    public string Nome { get; } = nome;
    public decimal Preco { get; } = preco;
    public bool Ativo { get; private set; } = ativo;

    public void Desativar() => Ativo = false;
}

/// <summary>Produtos semeados no catálogo em memória (os testes usam os ids).</summary>
public static class ProdutosConhecidos
{
    public static readonly Produto Teclado = new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Teclado mecânico", 350.00m);
    public static readonly Produto Mouse = new(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mouse sem fio", 120.00m);

    public static IEnumerable<Produto> Todos() =>
    [
        new(Teclado.Id, Teclado.Nome, Teclado.Preco),
        new(Mouse.Id, Mouse.Nome, Mouse.Preco),
    ];
}

public interface ICatalogo
{
    IReadOnlyList<Produto> ListarAtivos();
    Produto? Obter(Guid id);
    void Adicionar(Produto produto);
}

public sealed class CatalogoEmMemoria : ICatalogo
{
    private readonly ConcurrentDictionary<Guid, Produto> _produtos =
        new(ProdutosConhecidos.Todos().Select(p => KeyValuePair.Create(p.Id, p)));

    public IReadOnlyList<Produto> ListarAtivos() => [.. _produtos.Values.Where(p => p.Ativo).OrderBy(p => p.Nome)];

    public Produto? Obter(Guid id) => _produtos.GetValueOrDefault(id);

    public void Adicionar(Produto produto) => _produtos[produto.Id] = produto;
}
