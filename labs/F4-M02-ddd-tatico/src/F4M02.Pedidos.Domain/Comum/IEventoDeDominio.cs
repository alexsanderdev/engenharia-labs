namespace F4M02.Pedidos.Domain.Comum;

/// <summary>
/// Evento de domínio: um fato relevante para o negócio que JÁ aconteceu dentro de um agregado,
/// nomeado no passado e na linguagem ubíqua (<c>PedidoConfirmado</c>, não <c>StatusAtualizado</c>).
/// </summary>
/// <remarks>
/// PRONTO — leia. Eventos de domínio vivem dentro do bounded context e são despachados depois que o
/// agregado é salvo. Para outros contextos/sistemas, a camada de aplicação traduz para um
/// <i>integration event</i> (contrato público, versionado), normalmente via Outbox.
/// </remarks>
public interface IEventoDeDominio
{
    /// <summary>Instante (UTC) em que o fato ocorreu.</summary>
    DateTimeOffset OcorridoEm { get; }
}
