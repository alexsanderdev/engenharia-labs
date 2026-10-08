using F3M05.EfAvancado.Dominio;
using F3M05.EfAvancado.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace F3M05.EfAvancado.Tests.Infra;

/// <summary>
/// JÁ VEM PRONTA. Cada teste roda numa transação desfeita no fim (isolamento sem custo de limpeza).
/// Padrão dos testes: Arrange (semear) → <c>Sql.Limpar()</c> → Act → Assert no resultado E nos
/// comandos capturados.
/// </summary>
[Collection(ColecaoBanco.Nome)]
public abstract class BancoTestBase(SqlServerFixture fixture) : IAsyncLifetime
{
    private SqlConnection _conexao = null!;
    private SqlTransaction _transacao = null!;

    protected SqlServerFixture Fixture { get; } = fixture;
    protected FakeTimeProvider Relogio => Fixture.Relogio;
    protected CapturaDeSql Sql => Fixture.Sql;
    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _conexao = new SqlConnection(Fixture.ConnectionString);
        await _conexao.OpenAsync(Ct);
        _transacao = (SqlTransaction)await _conexao.BeginTransactionAsync(Ct);
        Sql.Limpar();
    }

    public async ValueTask DisposeAsync()
    {
        await _transacao.RollbackAsync();
        await _transacao.DisposeAsync();
        await _conexao.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// DbContext NOVO na conexão/transação do teste, com os interceptadores registrados
    /// na ordem: exclusão lógica → auditoria → captura de SQL.
    /// </summary>
    protected LojaDbContext NovoContexto()
    {
        var options = new DbContextOptionsBuilder<LojaDbContext>()
            .UseSqlServer(_conexao)
            .AddInterceptors(Fixture.ExclusaoLogica, Fixture.Auditoria, Fixture.Sql)
            .Options;
        var db = new LojaDbContext(options);
        db.Database.UseTransaction(_transacao);
        return db;
    }

    /// <summary>SQL puro na transação do teste (não passa pelo EF nem pela captura).</summary>
    protected async Task<object?> EscalarAsync(string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var cmd = new SqlCommand(sql, _conexao, _transacao);
        foreach (var (nome, valor) in parametros)
            cmd.Parameters.AddWithValue(nome, valor);
        var resultado = await cmd.ExecuteScalarAsync(Ct);
        return resultado is DBNull ? null : resultado;
    }

    /// <summary>SQL puro na transação do teste, devolvendo as linhas como dicionários.</summary>
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

    // ---------- Arrange ----------

    protected async Task<Produto> SemearProdutoAsync(string nome, decimal preco)
    {
        var produto = new Produto
        {
            Sku = new Sku($"P{Guid.NewGuid():N}"[..12]),
            Nome = nome,
            Preco = Dinheiro.Reais(preco),
        };
        await using var db = NovoContexto();
        db.Produtos.Add(produto);
        await db.SaveChangesAsync(Ct);
        return produto;
    }

    /// <summary>
    /// Cria um cliente com <paramref name="pedidos"/> pedidos; cada pedido recebe um item de cada
    /// produto informado (quantidade = posição + 1) e <paramref name="eventosPorPedido"/> eventos.
    /// </summary>
    protected async Task<Cliente> SemearClienteComPedidosAsync(
        string nome, int pedidos, IReadOnlyList<Produto> produtos, int eventosPorPedido = 0)
    {
        var cliente = new Cliente { Nome = nome };
        for (var p = 0; p < pedidos; p++)
        {
            var pedido = new Pedido();
            for (var i = 0; i < produtos.Count; i++)
                pedido.Itens.Add(new ItemPedido { ProdutoId = produtos[i].Id, Quantidade = i + 1, PrecoUnitario = 10m * (i + 1) });
            for (var e = 0; e < eventosPorPedido; e++)
                pedido.Eventos.Add(new EventoPedido { Descricao = $"Evento {e + 1}", OcorridoEm = Relogio.GetUtcNow() });
            cliente.Pedidos.Add(pedido);
        }

        await using var db = NovoContexto();
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(Ct);
        return cliente;
    }
}
