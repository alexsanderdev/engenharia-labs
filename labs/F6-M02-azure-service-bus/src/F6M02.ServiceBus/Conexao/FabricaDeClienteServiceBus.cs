using Azure.Core;
using Azure.Messaging.ServiceBus;

namespace F6M02.ServiceBus.Conexao;

/// <summary>
/// Configuração de conexão. Em desenvolvimento/testes (emulador) usa-se <see cref="ConnectionString"/>;
/// em produção, <see cref="FullyQualifiedNamespace"/> (ex.: <c>orderflow-prd.servicebus.windows.net</c>)
/// + Managed Identity, sem segredo nenhum na configuração.
/// </summary>
public sealed record ConexaoServiceBusOptions
{
    public string? ConnectionString { get; init; }

    public string? FullyQualifiedNamespace { get; init; }
}

/// <summary>
/// Cria o <see cref="ServiceBusClient"/> da aplicação (registre como SINGLETON: ele mantém a conexão AMQP).
/// </summary>
public static class FabricaDeClienteServiceBus
{
    /// <summary>
    /// Opções do cliente: transporte AMQP sobre TCP e retry exponencial do SDK
    /// (<c>Mode = Exponential</c>, <c>MaxRetries = 3</c>, <c>Delay = 800 ms</c>, <c>MaxDelay = 30 s</c>, <c>TryTimeout = 30 s</c>).
    /// </summary>
    public static ServiceBusClientOptions CriarOpcoesDoCliente() => new()
    {
        TransportType = ServiceBusTransportType.AmqpTcp,
        RetryOptions = new ServiceBusRetryOptions
        {
            Mode = ServiceBusRetryMode.Exponential,
            MaxRetries = 3,
            Delay = TimeSpan.FromMilliseconds(800),
            MaxDelay = TimeSpan.FromSeconds(30),
            TryTimeout = TimeSpan.FromSeconds(30),
        },
    };

    /// <summary>
    /// Regras:
    /// <list type="number">
    /// <item>connection string E namespace preenchidos → <see cref="InvalidOperationException"/> (configuração ambígua);</item>
    /// <item>connection string → <c>new ServiceBusClient(connectionString, opções)</c> (emulador, dev);</item>
    /// <item>namespace + <paramref name="credencial"/> → <c>new ServiceBusClient(namespace, credencial, opções)</c>
    /// (produção: passe <c>new DefaultAzureCredential()</c> do pacote Azure.Identity);</item>
    /// <item>qualquer outra combinação → <see cref="InvalidOperationException"/>.</item>
    /// </list>
    /// </summary>
    public static ServiceBusClient Criar(ConexaoServiceBusOptions opcoes, TokenCredential? credencial = null)
    {
        ArgumentNullException.ThrowIfNull(opcoes);
        var temConnectionString = !string.IsNullOrWhiteSpace(opcoes.ConnectionString);
        var temNamespace = !string.IsNullOrWhiteSpace(opcoes.FullyQualifiedNamespace);

        if (temConnectionString && temNamespace)
            throw new InvalidOperationException("Configure ConnectionString OU FullyQualifiedNamespace, não os dois.");

        if (temConnectionString)
            return new ServiceBusClient(opcoes.ConnectionString, CriarOpcoesDoCliente());

        if (temNamespace && credencial is not null)
            return new ServiceBusClient(opcoes.FullyQualifiedNamespace, credencial, CriarOpcoesDoCliente());

        throw new InvalidOperationException(
            "Service Bus sem configuração: informe ConnectionString (emulador/dev) ou FullyQualifiedNamespace + TokenCredential (Managed Identity).");
    }
}
