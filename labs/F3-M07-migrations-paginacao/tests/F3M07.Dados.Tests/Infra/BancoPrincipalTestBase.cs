using F3M07.Dados.Modelo;
using F3M07.Dados.Persistencia;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace F3M07.Dados.Tests.Infra;

/// <summary>Já vem pronta. Base dos testes que usam o banco principal (migrado e limpo antes de cada teste).</summary>
public abstract class BancoPrincipalTestBase(SqlServerFixture sql) : IAsyncLifetime
{
    protected SqlServerFixture Sql { get; } = sql;

    protected string ConnectionString => Sql.ConnectionStringPara(SqlServerFixture.BancoPrincipal);

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await Sql.PrepararBancoPrincipalAsync(Ct);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>Um DbContext NOVO (como em cada request HTTP), opcionalmente com interceptors.</summary>
    protected LojaDbContext NovoContexto(params IInterceptor[] interceptors) =>
        new(SqlServerFixture.Options(ConnectionString, interceptors));

    protected async Task<int> SemearClienteAsync(string nome = "Ana")
    {
        await using var db = NovoContexto();
        var cliente = new Cliente { Nome = nome };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(Ct);
        return cliente.Id;
    }

    protected async Task<Pedido> SemearPedidoAsync(int clienteId, decimal total, DateTime? criadoEm = null, StatusPedido status = StatusPedido.Created)
    {
        await using var db = NovoContexto();
        var pedido = new Pedido { ClienteId = clienteId, Total = total, CriadoEm = criadoEm ?? new DateTime(2026, 3, 1, 12, 0, 0), Status = status };
        db.Pedidos.Add(pedido);
        await db.SaveChangesAsync(Ct);
        return pedido;
    }
}
