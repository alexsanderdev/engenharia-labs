using F4M01.Application.Abstracoes;
using F4M01.Domain.Pedidos;
using F4M01.Domain.Produtos;
using Microsoft.Extensions.DependencyInjection;

namespace F4M01.Application.Tests;

// Fakes escritos à mão: implementar uma porta pequena custa poucas linhas.
// É o "adaptador de teste" da arquitetura hexagonal.

internal sealed class PedidoRepositoryFake : IPedidoRepository
{
    public List<Pedido> Salvos { get; } = [];

    public Task AdicionarAsync(Pedido pedido, CancellationToken ct)
    {
        Salvos.Add(pedido);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Pedido>> ListarPorClienteAsync(Guid clienteId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Pedido>>([.. Salvos.Where(p => p.ClienteId == clienteId)]);
}

internal sealed class ProdutoRepositoryFake(params Produto[] produtos) : IProdutoRepository
{
    public List<IReadOnlyCollection<Guid>> Consultas { get; } = [];

    public Task<IReadOnlyList<Produto>> ObterPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        Consultas.Add(ids);
        return Task.FromResult<IReadOnlyList<Produto>>([.. produtos.Where(p => ids.Contains(p.Id))]);
    }
}

internal sealed class RelogioFake(DateTimeOffset agora) : IRelogio
{
    public DateTimeOffset Agora { get; set; } = agora;
}

/// <summary>
/// Monta um caso de uso oferecendo SÓ as portas (fakes). Se o handler pedir qualquer outra coisa
/// no construtor (um DbContext, uma classe concreta da Infrastructure), a montagem falha — e o teste diz por quê.
/// </summary>
internal static class Montar
{
    public static T CasoDeUso<T>(IPedidoRepository pedidos, IProdutoRepository? produtos = null, IRelogio? relogio = null)
        where T : class
    {
        var services = new ServiceCollection()
            .AddSingleton(pedidos)
            .AddSingleton(produtos ?? new ProdutoRepositoryFake())
            .AddSingleton(relogio ?? new RelogioFake(DateTimeOffset.UnixEpoch));

        using var provider = services.BuildServiceProvider();
        try
        {
            return ActivatorUtilities.CreateInstance<T>(provider);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException(
                $"{typeof(T).Name} deveria depender apenas das portas da Application (IPedidoRepository, IProdutoRepository, IRelogio). {ex.Message}", ex);
        }
    }
}
