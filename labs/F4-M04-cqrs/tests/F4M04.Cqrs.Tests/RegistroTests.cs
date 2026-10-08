using F4M04.Cqrs.Abstractions;
using F4M04.Cqrs.Dispatching;
using F4M04.Cqrs.Pedidos.Dominio;
using F4M04.Cqrs.Pedidos.Escrita;
using F4M04.Cqrs.Pedidos.Leitura;
using F4M04.Cqrs.Pipeline;
using F4M04.Cqrs.Tests.Infra;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace F4M04.Cqrs.Tests;

/// <summary>Passo 2: AddCqrs registra tudo por varredura de assembly, já com os decorators.</summary>
public sealed class RegistroTests
{
    [Fact]
    public async Task AddCqrs_VarreOAssembly_RegistraHandlersDecoradosValidatorsEProjecao()
    {
        await using var ambiente = new Ambiente();
        await using var scope = ambiente.Provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;

        // Handlers dos quatro casos de uso, resolvidos pela interface: o objeto de FORA é o decorator de log.
        sp.GetRequiredService<ICommandHandler<CriarPedido, Guid>>().ShouldBeOfType<LoggingCommandDecorator<CriarPedido, Guid>>();
        sp.GetRequiredService<ICommandHandler<ConfirmarPedido, Unit>>().ShouldBeOfType<LoggingCommandDecorator<ConfirmarPedido, Unit>>();
        sp.GetRequiredService<IQueryHandler<ObterPedido, PedidoResumo?>>().ShouldBeOfType<LoggingQueryDecorator<ObterPedido, PedidoResumo?>>();
        sp.GetRequiredService<IQueryHandler<ListarPedidosDoCliente, IReadOnlyList<PedidoResumo>>>()
            .ShouldBeOfType<LoggingQueryDecorator<ListarPedidosDoCliente, IReadOnlyList<PedidoResumo>>>();

        // Validators e handlers de eventos de domínio também vêm da varredura.
        sp.GetServices<IValidator<CriarPedido>>().ShouldHaveSingleItem().ShouldBeOfType<CriarPedidoValidator>();
        sp.GetServices<IValidator<ListarPedidosDoCliente>>().ShouldHaveSingleItem();
        sp.GetServices<IDomainEventHandler<PedidoCriado>>().ShouldHaveSingleItem().ShouldBeOfType<ProjecaoDePedidos>();
        sp.GetServices<IDomainEventHandler<PedidoConfirmado>>().ShouldHaveSingleItem().ShouldBeOfType<ProjecaoDePedidos>();
    }

    [Fact]
    public async Task AddCqrs_ChamadoDuasVezes_RegistraUmDispatcherSo()
    {
        await using var ambiente = new Ambiente(comTiposDeTeste: true);

        ambiente.Provider.CreateScope().ServiceProvider.GetServices<IDispatcher>().ShouldHaveSingleItem().ShouldBeOfType<Dispatcher>();
    }

    [Fact]
    public void AddCqrs_DoisHandlersParaOMesmoCommand_LancaInvalidOperation()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() => services.AddCqrs(TiposDeTeste.Duplicados));

        ex.Message.ShouldContain(nameof(ComandoDuplicado));
    }
}
