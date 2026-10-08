using F6M08.Consistencia.Dominio;
using F6M08.Consistencia.Projecao;

namespace F6M08.Consistencia.Mensageria;

/// <summary>
/// Consumidor que leva os eventos da fila para a projeção.
/// <para>
/// Na API ele roda como <see cref="BackgroundService"/>. Nos testes de unidade, o teste chama
/// <see cref="ProcessarDisponiveis"/> logo depois de avançar o relógio — sem thread de fundo, sem espera.
/// </para>
/// Pronto: leia, não altere.
/// </summary>
public sealed class Projetor(FilaComAtraso<EventoDePedido> fila, ProjecaoResumoDoCliente projecao, ILogger<Projetor>? logger = null)
    : BackgroundService
{
    /// <summary>Aplica tudo o que já está disponível na fila. Devolve quantos eventos consumiu.</summary>
    public int ProcessarDisponiveis()
    {
        var consumidos = 0;
        while (fila.Leitor.TryRead(out var evento))
        {
            var resultado = projecao.Aplicar(evento);
            consumidos++;
            logger?.LogDebug("Evento {Tipo} v{Versao} do pedido {PedidoId}: {Resultado}",
                evento.GetType().Name, evento.Versao, evento.PedidoId, resultado);
        }
        return consumidos;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await fila.Leitor.WaitToReadAsync(stoppingToken))
            {
                try
                {
                    ProcessarDisponiveis();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Num consumidor real: retry/DLQ (módulo 6.05). Aqui: registrar e seguir.
                    logger?.LogError(ex, "Falha ao projetar evento.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // desligamento normal
        }
    }
}
