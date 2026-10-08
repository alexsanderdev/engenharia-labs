using F4M04.Cqrs.Abstractions;

namespace F4M04.Cqrs.Pedidos.Infra;

/// <summary>
/// Unidade de trabalho em memória (scoped):
/// 1) grava no <see cref="BancoDeEscrita"/> todos os agregados rastreados;
/// 2) só DEPOIS publica os eventos de domínio que eles registraram
///    (é isso que atualiza o read model, de forma síncrona e no mesmo processo).
/// </summary>
public sealed class UnitOfWorkEmMemoria(
    RepositorioDePedidos repositorio,
    BancoDeEscrita banco,
    IDomainEventPublisher publicador) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        var rastreados = repositorio.Rastreados.ToList();
        foreach (var pedido in rastreados)
            banco.Pedidos[pedido.Id] = pedido;
        banco.RegistrarCommit();

        var eventos = rastreados.SelectMany(p => p.RetirarEventos()).OrderBy(e => e.OcorreuEm).ToList();
        repositorio.LimparRastreamento();

        foreach (var evento in eventos)
            await publicador.PublishAsync(evento, ct);
    }
}
