using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace F1M06.Hosting;

/// <summary>Opções de pedidos, lidas da seção "Pedidos" da configuração (detalhes de Options no módulo 1.08).</summary>
public sealed class OpcoesPedidos
{
    public const string Secao = "Pedidos";

    /// <summary>Prefixo do código do pedido.</summary>
    public string Prefixo { get; set; } = "PED";

    /// <summary>De quanto em quanto tempo o worker de limpeza roda.</summary>
    public TimeSpan IntervaloLimpeza { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Pedido "Criado" há mais que isso é cancelado pela limpeza.</summary>
    public int MinutosParaExpirar { get; set; } = 30;
}

/// <summary>Gera códigos de pedido sequenciais, ex.: PED-2026-000001.</summary>
public interface IGeradorDeCodigoPedido
{
    string Proximo();
}

/// <summary>Pedido aguardando pagamento.</summary>
public sealed class PedidoPendente(Guid id, DateTimeOffset criadoEm)
{
    public Guid Id { get; } = id;
    public DateTimeOffset CriadoEm { get; } = criadoEm;
    public bool Cancelado { get; set; }
}

/// <summary>Repositório de pedidos pendentes.</summary>
public interface IRepositorioPedidos
{
    void Adicionar(PedidoPendente pedido);
    IReadOnlyList<PedidoPendente> Todos();
}

/// <summary>Repositório em memória (estado compartilhado, thread-safe → Singleton).</summary>
public sealed class RepositorioPedidosEmMemoria : IRepositorioPedidos
{
    private readonly ConcurrentDictionary<Guid, PedidoPendente> _pedidos = new();

    public void Adicionar(PedidoPendente pedido) => _pedidos[pedido.Id] = pedido;

    public IReadOnlyList<PedidoPendente> Todos() => [.. _pedidos.Values];
}

/// <summary>Regras de pedido usadas pelo worker de limpeza.</summary>
public interface IServicoDePedidos
{
    /// <summary>Cancela pedidos não cancelados criados há mais de <see cref="OpcoesPedidos.MinutosParaExpirar"/>.</summary>
    Task<int> CancelarExpiradosAsync(CancellationToken cancellationToken);
}

/// <summary>Implementação real (Scoped: em um sistema com banco, dependeria do DbContext, que é Scoped).</summary>
public sealed class ServicoDePedidos(
    IRepositorioPedidos repositorio,
    TimeProvider tempo,
    IOptions<OpcoesPedidos> opcoes,
    ILogger<ServicoDePedidos> logger) : IServicoDePedidos
{
    public Task<int> CancelarExpiradosAsync(CancellationToken cancellationToken)
    {
        var limite = tempo.GetUtcNow() - TimeSpan.FromMinutes(opcoes.Value.MinutosParaExpirar);
        var cancelados = 0;

        foreach (var pedido in repositorio.Todos().Where(p => !p.Cancelado && p.CriadoEm < limite))
        {
            cancellationToken.ThrowIfCancellationRequested();
            pedido.Cancelado = true;
            cancelados++;
        }

        logger.LogInformation("Limpeza cancelou {Quantidade} pedidos expirados", cancelados);
        return Task.FromResult(cancelados);
    }
}
