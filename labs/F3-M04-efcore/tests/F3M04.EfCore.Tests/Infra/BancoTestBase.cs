using F3M04.EfCore.Dominio;
using F3M04.EfCore.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace F3M04.EfCore.Tests.Infra;

/// <summary>
/// JÁ VEM PRONTA. Isolamento por transação: cada teste abre UMA conexão, inicia uma transação
/// e todos os <see cref="OrderFlowDbContext"/> do teste usam essa conexão/transação.
/// No fim do teste, rollback — o banco volta ao estado inicial (com o seed) sem custo de limpeza.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public abstract class BancoTestBase(SqlServerFixture fixture) : IAsyncLifetime
{
    private SqlConnection _conexao = null!;
    private SqlTransaction _transacao = null!;

    protected SqlServerFixture Fixture { get; } = fixture;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Relógio controlado para o <c>PedidoService</c>.</summary>
    protected FakeTimeProvider Relogio { get; } = new(new DateTimeOffset(2026, 3, 10, 12, 0, 0, TimeSpan.Zero));

    public async ValueTask InitializeAsync()
    {
        _conexao = new SqlConnection(Fixture.ConnectionString);
        await _conexao.OpenAsync(Ct);
        _transacao = (SqlTransaction)await _conexao.BeginTransactionAsync(Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _transacao.RollbackAsync();
        await _transacao.DisposeAsync();
        await _conexao.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Um DbContext NOVO (change tracker vazio) na conexão/transação do teste.
    /// Use um para o Arrange, outro para o Act e outro para o Assert.
    /// </summary>
    protected OrderFlowDbContext NovoContexto()
    {
        var options = new DbContextOptionsBuilder<OrderFlowDbContext>()
            .UseSqlServer(_conexao)
            .Options;
        var db = new OrderFlowDbContext(options);
        db.Database.UseTransaction(_transacao);
        return db;
    }

    /// <summary>Executa SQL puro na transação do teste e devolve o primeiro valor (ou null).</summary>
    protected async Task<object?> EscalarAsync(string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var cmd = new SqlCommand(sql, _conexao, _transacao);
        foreach (var (nome, valor) in parametros)
            cmd.Parameters.AddWithValue(nome, valor);
        var resultado = await cmd.ExecuteScalarAsync(Ct);
        return resultado is DBNull ? null : resultado;
    }

    /// <summary>Executa SQL puro na transação do teste e devolve as linhas como dicionários.</summary>
    protected async Task<List<Dictionary<string, object?>>> LinhasAsync(string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var cmd = new SqlCommand(sql, _conexao, _transacao);
        foreach (var (nome, valor) in parametros)
            cmd.Parameters.AddWithValue(nome, valor);
        await using var reader = await cmd.ExecuteReaderAsync(Ct);
        var linhas = new List<Dictionary<string, object?>>();
        while (await reader.ReadAsync(Ct))
        {
            var linha = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
                linha[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            linhas.Add(linha);
        }
        return linhas;
    }

    // ---------- Arrange: dados de teste gravados direto pelo EF ----------

    protected async Task<Cliente> SemearClienteAsync(string nome = "Ana")
    {
        var cliente = new Cliente(Guid.NewGuid(), nome, $"{nome.ToLowerInvariant()}.{Guid.NewGuid():N}@exemplo.com");
        await using var db = NovoContexto();
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(Ct);
        return cliente;
    }

    protected async Task<Produto> SemearProdutoAsync(string nome, decimal preco, bool ativo = true)
    {
        var produto = new Produto(Guid.NewGuid(), $"SKU-{Guid.NewGuid():N}"[..20], nome, preco, ativo);
        await using var db = NovoContexto();
        db.Produtos.Add(produto);
        await db.SaveChangesAsync(Ct);
        return produto;
    }

    /// <summary>Grava um pedido com os itens informados (produto, quantidade) na data indicada.</summary>
    protected async Task<Pedido> SemearPedidoAsync(Cliente cliente, DateTimeOffset criadoEm, params (Produto Produto, int Quantidade)[] itens)
    {
        var pedido = new Pedido(cliente.Id, criadoEm);
        foreach (var (produto, quantidade) in itens)
            pedido.AdicionarItem(produto, quantidade);

        await using var db = NovoContexto();
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(Ct);
        return pedido;
    }
}
