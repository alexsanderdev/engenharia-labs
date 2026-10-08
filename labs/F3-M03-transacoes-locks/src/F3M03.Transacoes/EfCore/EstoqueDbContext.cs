using Microsoft.EntityFrameworkCore;

namespace F3M03.Transacoes.EfCore;

/// <summary>Produto mapeado para <c>dbo.Produtos</c> (a mesma tabela usada pelo ADO.NET).</summary>
public sealed class Produto
{
    public int Id { get; set; }
    public string Sku { get; set; } = "";
    public string Nome { get; set; } = "";
    public decimal Preco { get; set; }
    public int Estoque { get; set; }

    /// <summary>
    /// Coluna <c>rowversion</c>: o SQL Server muda o valor a cada UPDATE da linha. Configurada como
    /// token de concorrência, o EF Core passa a gerar <c>UPDATE ... WHERE Id = @id AND Versao = @versaoOriginal</c>
    /// e lança <see cref="DbUpdateConcurrencyException"/> quando nenhuma linha é afetada.
    /// </summary>
    public byte[] Versao { get; set; } = [];
}

/// <summary>Pedido mapeado para <c>dbo.Pedidos</c> (FK para Clientes e Produtos).</summary>
public sealed class Pedido
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
}

/// <summary>
/// DbContext PRONTO, mapeando as tabelas que a fixture de testes cria por script
/// (por isso não há migrations neste lab).
/// </summary>
public sealed class EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();

    /// <summary>Atalho para criar um contexto apontando para <paramref name="connectionString"/>.</summary>
    public static EstoqueDbContext Criar(string connectionString) =>
        new(new DbContextOptionsBuilder<EstoqueDbContext>().UseSqlServer(connectionString).Options);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Produto>(p =>
        {
            p.ToTable("Produtos");
            p.HasKey(x => x.Id);
            p.Property(x => x.Id).ValueGeneratedNever();
            p.Property(x => x.Sku).HasMaxLength(30).IsUnicode(false);
            p.Property(x => x.Nome).HasMaxLength(100);
            p.Property(x => x.Preco).HasPrecision(18, 2);
            p.Property(x => x.Versao).IsRowVersion(); // = [Timestamp]
        });

        modelBuilder.Entity<Pedido>(p =>
        {
            p.ToTable("Pedidos");
            p.HasKey(x => x.Id); // IDENTITY no banco
        });
    }
}
