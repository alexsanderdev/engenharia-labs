using F6M06.Worker.Expiracao;
using F6M06.Worker.Notificacoes;
using F6M06.Worker.Pedidos;
using F6M06.Worker.Saude;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace F6M06.Worker;

/// <summary>Composição dos workers do OrderFlow.</summary>
public static class DependencyInjection
{
    /// <summary>Tag dos health checks de workers.</summary>
    public const string TagWorker = "worker";

    /// <summary>
    /// Registra:
    /// <list type="bullet">
    /// <item>options <see cref="ExpiracaoOptions"/> e <see cref="NotificacoesOptions"/> ligadas às seções
    /// <c>Expiracao</c>/<c>Notificacoes</c>, com <c>ValidateDataAnnotations()</c> e <c>ValidateOnStart()</c>;</item>
    /// <item><c>TryAddSingleton(TimeProvider.System)</c> (os testes trocam por um relógio falso);</item>
    /// <item>singletons: <see cref="BancoEmMemoria"/>, <see cref="MonitorDeWorkers"/>, <see cref="FilaDeNotificacoes"/>;
    /// <c>TryAddSingleton&lt;IEnviadorDeNotificacoes, EnviadorDeNotificacoesNoLog&gt;</c>;</item>
    /// <item>scoped: <see cref="IRepositorioDePedidos"/> → <see cref="RepositorioDePedidosEmMemoria"/> e <see cref="ServicoDeExpiracao"/>;</item>
    /// <item>hosted services: <see cref="ExpiracaoDePedidosWorker"/> e <see cref="ProcessadorDeNotificacoesWorker"/>;</item>
    /// <item>health check <see cref="ExpiracaoDePedidosWorker.Nome"/> com tag <see cref="TagWorker"/>, criando um
    /// <see cref="WorkerHealthCheck"/> com a <see cref="ExpiracaoOptions.ToleranciaSemBatimento"/>.</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddWorkersDoOrderFlow(this IServiceCollection services, IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(configuracao);

        throw new NotImplementedException(
            "TODO (Passo 1): options com Bind/ValidateDataAnnotations/ValidateOnStart, TryAddSingleton(TimeProvider.System), " +
            "singletons, scoped, AddHostedService dos 2 workers e AddHealthChecks().Add(new HealthCheckRegistration(...))");
    }
}
