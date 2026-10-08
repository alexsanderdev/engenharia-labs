using System.Collections.Concurrent;

namespace F5M03.Api.Dominio;

// ARQUIVO PRONTO — não precisa alterar.
// Repositórios em memória. O de produtos SIMULA um banco SQL: ele monta o texto da
// consulta concatenando a coluna de ordenação (como muito código legado faz com ORDER BY)
// e, quando a coluna não existe, lança a mesma mensagem que o SQL Server lançaria,
// com o SQL dentro. É exatamente o tipo de detalhe que não pode chegar ao cliente.

public interface IProdutoRepositorio
{
    /// <summary>Lista os produtos ativos ordenados pela coluna informada (nome da coluna no banco).</summary>
    IReadOnlyList<Produto> ListarAtivos(string colunaDeOrdenacao);

    Produto? Obter(Guid id);
}

public interface IClienteRepositorio
{
    void Adicionar(Cliente cliente);
    Cliente? Obter(Guid id);
    IReadOnlyList<Cliente> Todos();
}

public interface IPedidoRepositorio
{
    void Adicionar(Pedido pedido);
    Pedido? Obter(Guid id);
    IReadOnlyList<Pedido> Todos();
}

/// <summary>Produtos semeados (ids fixos para os testes e para o arquivo .http).</summary>
public static class ProdutosConhecidos
{
    public static readonly Guid TecladoId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid MouseId = Guid.Parse("11111111-0000-0000-0000-000000000002");
    public static readonly Guid MonitorId = Guid.Parse("11111111-0000-0000-0000-000000000003");
    public static readonly Guid WebcamInativaId = Guid.Parse("11111111-0000-0000-0000-000000000004");

    public static IEnumerable<Produto> Semente() =>
    [
        new() { Id = TecladoId, Nome = "Teclado mecânico", Preco = 199.90m, CustoInterno = 120.00m },
        new() { Id = MouseId, Nome = "Mouse sem fio", Preco = 99.90m, CustoInterno = 41.50m },
        new() { Id = MonitorId, Nome = "Monitor 27\"", Preco = 1299.00m, CustoInterno = 910.00m },
        new() { Id = WebcamInativaId, Nome = "Webcam HD", Preco = 249.00m, CustoInterno = 150.00m, Ativo = false },
    ];
}

public sealed class ProdutoRepositorioEmMemoria : IProdutoRepositorio
{
    private readonly ConcurrentDictionary<Guid, Produto> _produtos =
        new(ProdutosConhecidos.Semente().Select(p => KeyValuePair.Create(p.Id, p)));

    public IReadOnlyList<Produto> ListarAtivos(string colunaDeOrdenacao)
    {
        // Simulação de: $"SELECT ... ORDER BY {colunaDeOrdenacao}" — concatenar entrada do
        // cliente em SQL é injeção esperando para acontecer. A defesa é allowlist ANTES daqui.
        var sql = $"SELECT Id, Nome, Preco, CustoInterno FROM dbo.Produtos WHERE Ativo = 1 ORDER BY {colunaDeOrdenacao}";

        Func<Produto, object> chave = colunaDeOrdenacao.Trim().ToUpperInvariant() switch
        {
            "NOME" => p => p.Nome,
            "PRECO" => p => p.Preco,
            "CUSTOINTERNO" => p => p.CustoInterno,
            _ => throw new InvalidOperationException(
                $"Invalid column name '{colunaDeOrdenacao}'. Statement(s) could not be prepared. " +
                $"Server: sql-prod-01.orderflow.internal, Database: OrderFlow. SQL: {sql}"),
        };

        return [.. _produtos.Values.Where(p => p.Ativo).OrderBy(chave)];
    }

    public Produto? Obter(Guid id) => _produtos.GetValueOrDefault(id);
}

public sealed class ClienteRepositorioEmMemoria : IClienteRepositorio
{
    private readonly ConcurrentDictionary<Guid, Cliente> _clientes = new();

    public void Adicionar(Cliente cliente) => _clientes[cliente.Id] = cliente;
    public Cliente? Obter(Guid id) => _clientes.GetValueOrDefault(id);
    public IReadOnlyList<Cliente> Todos() => [.. _clientes.Values];
}

public sealed class PedidoRepositorioEmMemoria : IPedidoRepositorio
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public void Adicionar(Pedido pedido) => _pedidos[pedido.Id] = pedido;
    public Pedido? Obter(Guid id) => _pedidos.GetValueOrDefault(id);
    public IReadOnlyList<Pedido> Todos() => [.. _pedidos.Values];
}
