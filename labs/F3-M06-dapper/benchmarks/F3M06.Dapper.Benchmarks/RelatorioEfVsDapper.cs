using BenchmarkDotNet.Attributes;
using Dapper;
using F3M06.Dapper.Escrita;
using F3M06.Dapper.Leitura;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace F3M06.Dapper.Benchmarks;

/// <summary>
/// Mesmo relatório de vendas por dia (um mês, ~20 mil pedidos, ~60 mil itens) de quatro jeitos:
///  1. EF Core "ingênuo": carrega pedidos + itens e agrega em memória (o erro clássico);
///  2. EF Core com GroupBy traduzido para SQL (o jeito certo no EF);
///  3. Dapper com o SQL de referência;
///  4. Dapper com o SEU Sql/RelatorioDeVendas.sql (via PedidoQueries).
/// Compare tempo E alocação: a diferença entre 2 e 3 costuma ser bem menor do que a entre 1 e 2.
/// </summary>
[MemoryDiagnoser]
public class RelatorioEfVsDapper
{
    private const string NomeDoBanco = "F3M06Bench";
    private const int TotalPedidos = 20_000;

    private static readonly DateTime Inicio = new(2026, 3, 1);
    private static readonly DateTime FimExclusivo = new(2026, 4, 1);

    private const string SqlDeReferencia = """
        SELECT CAST(p.CriadoEm AS date) AS Dia,
               COUNT(DISTINCT p.Id) AS QuantidadePedidos,
               SUM(i.Quantidade) AS ItensVendidos,
               SUM(i.Quantidade * i.PrecoUnitario) AS Faturamento
        FROM Pedidos AS p
        INNER JOIN ItensPedido AS i ON i.PedidoId = p.Id
        WHERE p.CriadoEm >= @Inicio AND p.CriadoEm < @FimExclusivo AND p.Status <> 'Cancelled'
        GROUP BY CAST(p.CriadoEm AS date)
        ORDER BY Dia;
        """;

    private MsSqlContainer? _container;
    private string _connectionString = "";
    private DbContextOptions<LojaDbContext> _options = null!;
    private PedidoQueries _pedidoQueries = null!;

    [GlobalSetup]
    public async Task PrepararAsync()
    {
        var externa = Environment.GetEnvironmentVariable("F3M06_BENCH_CONNECTION");
        if (string.IsNullOrWhiteSpace(externa))
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
            await _container.StartAsync();
            externa = _container.GetConnectionString();
        }

        _connectionString = new SqlConnectionStringBuilder(externa) { InitialCatalog = NomeDoBanco }.ConnectionString;
        _options = new DbContextOptionsBuilder<LojaDbContext>().UseSqlServer(_connectionString).Options;
        _pedidoQueries = new PedidoQueries(_connectionString);

        await using (var db = new LojaDbContext(_options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }

        await SemearAsync();
    }

    /// <summary>Massa gerada no próprio SQL Server (set-based): segundos em vez de minutos.</summary>
    private async Task SemearAsync()
    {
        const string sql = """
            INSERT INTO Clientes (Id, Nome, Email)
            SELECT NEWID(), CONCAT('Cliente ', n), CONCAT('cliente', n, '@orderflow.dev')
            FROM (SELECT TOP (100) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects) AS t;

            INSERT INTO Produtos (Id, Sku, Nome, Preco, Ativo)
            SELECT NEWID(), CONCAT('SKU-', n), CONCAT('Produto ', n), 10 + n, 1
            FROM (SELECT TOP (50) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n FROM sys.all_objects) AS t;

            SELECT TOP (@TotalPedidos) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
            INTO #numeros
            FROM sys.all_objects AS a CROSS JOIN sys.all_objects AS b;

            INSERT INTO Pedidos (Id, ClienteId, CriadoEm, Status, Total)
            SELECT NEWID(),
                   (SELECT TOP (1) Id FROM Clientes ORDER BY CHECKSUM(NEWID(), n.n)),
                   DATEADD(MINUTE, n.n * 2, '2026-03-01'),
                   CASE WHEN n.n % 10 = 0 THEN 'Cancelled' ELSE 'Completed' END,
                   0
            FROM #numeros AS n;

            INSERT INTO ItensPedido (PedidoId, ProdutoId, Quantidade, PrecoUnitario)
            SELECT p.Id, pr.Id, 1 + ABS(CHECKSUM(NEWID())) % 3, pr.Preco
            FROM Pedidos AS p
            CROSS APPLY (SELECT TOP (3) Id, Preco FROM Produtos ORDER BY CHECKSUM(NEWID(), p.Id)) AS pr;

            UPDATE p SET Total = t.Total
            FROM Pedidos AS p
            INNER JOIN (SELECT PedidoId, SUM(Quantidade * PrecoUnitario) AS Total FROM ItensPedido GROUP BY PedidoId) AS t
                ON t.PedidoId = p.Id;

            UPDATE STATISTICS Pedidos;
            UPDATE STATISTICS ItensPedido;
            """;

        await using var conexao = new SqlConnection(_connectionString);
        await conexao.ExecuteAsync(sql, new { TotalPedidos }, commandTimeout: 300);
    }

    [GlobalCleanup]
    public async Task LimparAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }

    [Benchmark(Description = "EF Core: carrega tudo e agrega em memória")]
    public async Task<int> EfCoreCarregaEAgregaEmMemoria()
    {
        await using var db = new LojaDbContext(_options);
        var pedidos = await db.Pedidos
            .Include(p => p.Itens)
            .Where(p => p.CriadoEm >= Inicio && p.CriadoEm < FimExclusivo && p.Status != StatusPedido.Cancelled)
            .ToListAsync();

        return pedidos
            .GroupBy(p => p.CriadoEm.Date)
            .Select(g => new VendasPorDia
            {
                Dia = g.Key,
                QuantidadePedidos = g.Count(),
                ItensVendidos = g.Sum(p => p.Itens.Sum(i => i.Quantidade)),
                Faturamento = g.Sum(p => p.Itens.Sum(i => i.Quantidade * i.PrecoUnitario)),
            })
            .Count();
    }

    [Benchmark(Baseline = true, Description = "EF Core: GroupBy traduzido, AsNoTracking")]
    public async Task<int> EfCoreGroupByTraduzido()
    {
        await using var db = new LojaDbContext(_options);
        var linhas = await db.ItensPedido
            .AsNoTracking()
            .Where(i => i.Pedido!.CriadoEm >= Inicio && i.Pedido.CriadoEm < FimExclusivo && i.Pedido.Status != StatusPedido.Cancelled)
            .GroupBy(i => i.Pedido!.CriadoEm.Date)
            .Select(g => new VendasPorDia
            {
                Dia = g.Key,
                QuantidadePedidos = g.Select(i => i.PedidoId).Distinct().Count(),
                ItensVendidos = g.Sum(i => i.Quantidade),
                Faturamento = g.Sum(i => i.Quantidade * i.PrecoUnitario),
            })
            .OrderBy(v => v.Dia)
            .ToListAsync();

        return linhas.Count;
    }

    [Benchmark(Description = "Dapper: SQL de referência")]
    public async Task<int> DapperSqlDeReferencia()
    {
        await using var conexao = new SqlConnection(_connectionString);
        var linhas = await conexao.QueryAsync<VendasPorDia>(SqlDeReferencia, new { Inicio, FimExclusivo });
        return linhas.AsList().Count;
    }

    [Benchmark(Description = "Dapper: o SEU Sql/RelatorioDeVendas.sql")]
    public async Task<int> DapperSqlDoLab()
    {
        var linhas = await _pedidoQueries.RelatorioDeVendasAsync(DateOnly.FromDateTime(Inicio), new DateOnly(2026, 3, 31));
        return linhas.Count;
    }
}
