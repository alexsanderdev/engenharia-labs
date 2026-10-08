using F4M01.Application.Abstracoes;

namespace F4M01.Infrastructure.Tempo;

/// <summary>ADAPTADOR da porta <see cref="IRelogio"/>: o relógio "de verdade".</summary>
internal sealed class RelogioDoSistema(TimeProvider timeProvider) : IRelogio
{
    public DateTimeOffset Agora => timeProvider.GetUtcNow();
}
