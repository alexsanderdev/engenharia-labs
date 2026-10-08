using System.Reflection;
using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Pedidos.Escrita;
using F4M04.Cqrs.Pedidos.Infra;
using F4M04.Cqrs.Pedidos.Leitura;
using F4M04.Cqrs.Tests.Infra;
using Microsoft.Extensions.DependencyInjection;

namespace F4M04.Cqrs.Tests;

/// <summary>Passos 6 e 7: o lado de leitura é separado, não grava nada e só vê o que foi confirmado.</summary>
public sealed class LeituraTests
{
    private static readonly Guid Ana = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Bruno = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    [Fact]
    public async Task ListarPedidosDoCliente_TrazSoOsDoClienteMaisRecentesPrimeiro()
    {
        await using var ambiente = new Ambiente();
        var primeiro = await ambiente.EnviarAsync(new CriarPedido(Ana, [new ItemSolicitado(ProdutosConhecidos.Mouse.Id, 1)]));
        ambiente.Relogio.Advance(TimeSpan.FromMinutes(5));
        await ambiente.EnviarAsync(new CriarPedido(Bruno, [new ItemSolicitado(ProdutosConhecidos.Mouse.Id, 1)]));
        ambiente.Relogio.Advance(TimeSpan.FromMinutes(5));
        var terceiro = await ambiente.EnviarAsync(new CriarPedido(Ana, [new ItemSolicitado(ProdutosConhecidos.Teclado.Id, 1)]));

        var daAna = await ambiente.ConsultarAsync(new ListarPedidosDoCliente(Ana));

        daAna.Select(p => p.Id).ShouldBe([terceiro, primeiro]);
    }

    [Fact]
    public async Task Queries_ExecutadasVariasVezes_NaoAlteramNenhumEstado()
    {
        await using var ambiente = new Ambiente();
        var id = await ambiente.EnviarAsync(new CriarPedido(Ana, [new ItemSolicitado(ProdutosConhecidos.Mouse.Id, 2)]));
        await ambiente.EnviarAsync(new ConfirmarPedido(id));
        var commitsAntes = ambiente.Escrita.Commits;
        var leituraAntes = ambiente.Leitura.Pedidos.ToDictionary();

        for (var i = 0; i < 3; i++)
        {
            await ambiente.ConsultarAsync(new ObterPedido(id));
            await ambiente.ConsultarAsync(new ObterPedido(Guid.NewGuid()));
            await ambiente.ConsultarAsync(new ListarPedidosDoCliente(Ana));
        }

        ambiente.Escrita.Commits.ShouldBe(commitsAntes);
        ambiente.Leitura.Pedidos.ToDictionary().ShouldBe(leituraAntes, ignoreOrder: true);
        (await ambiente.ConsultarAsync(new ObterPedido(Guid.NewGuid()))).ShouldBeNull();
    }

    [Fact]
    public void QueryHandlers_DependemSoDoLadoDeLeitura()
    {
        Type[] proibidos = [typeof(IRepositorioDePedidos), typeof(RepositorioDePedidos), typeof(ICatalogo), typeof(IUnitOfWork), typeof(BancoDeEscrita)];
        var queryHandlers = typeof(CriarPedido).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
            .ToList();

        queryHandlers.Count.ShouldBe(2);
        foreach (var handler in queryHandlers)
        {
            var dependencias = handler.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType)
                .ToList();
            dependencias.ShouldContain(typeof(BancoDeLeitura), $"{handler.Name} deveria ler do BancoDeLeitura.");
            dependencias.Intersect(proibidos).ShouldBeEmpty($"{handler.Name} não pode depender do lado de escrita.");
        }
    }

    [Fact]
    public async Task ReadModel_SoEAtualizadoDepoisDoCommitDaUnidadeDeTrabalho()
    {
        await using var ambiente = new Ambiente();
        await using var scope = ambiente.Provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var ct = TestContext.Current.CancellationToken;

        // Handler "cru", sem o pipeline: ele só registra o agregado no repositório.
        var handlerCru = sp.GetRequiredService<CriarPedidoHandler>();
        var id = await handlerCru.HandleAsync(new CriarPedido(Ana, [new ItemSolicitado(ProdutosConhecidos.Mouse.Id, 1)]), ct);

        ambiente.Leitura.Pedidos.ShouldNotContainKey(id);
        (await ambiente.ConsultarAsync(new ObterPedido(id))).ShouldBeNull();

        await sp.GetRequiredService<IUnitOfWork>().SaveChangesAsync(ct);

        (await ambiente.ConsultarAsync(new ObterPedido(id))).ShouldNotBeNull().Status.ShouldBe("Created");
    }
}
