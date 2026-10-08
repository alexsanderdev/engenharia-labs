using Azure.Messaging.ServiceBus;

namespace F6M02.ServiceBus.Consumo;

/// <summary>
/// De onde um consumidor lê: uma fila OU uma subscription de um tópico.
/// Os helpers criam processor/receiver do tipo certo para cada caso.
/// </summary>
public sealed record OrigemDasMensagens
{
    private OrigemDasMensagens(string entidade, string? subscription)
    {
        Entidade = entidade;
        Subscription = subscription;
    }

    /// <summary>Nome da fila ou do tópico.</summary>
    public string Entidade { get; }

    /// <summary>Nome da subscription (null quando a origem é uma fila).</summary>
    public string? Subscription { get; }

    public static OrigemDasMensagens Fila(string fila) => new(fila, null);

    public static OrigemDasMensagens DaSubscription(string topico, string subscription) => new(topico, subscription);

    public ServiceBusProcessor CriarProcessor(ServiceBusClient cliente, ServiceBusProcessorOptions opcoes) =>
        Subscription is null
            ? cliente.CreateProcessor(Entidade, opcoes)
            : cliente.CreateProcessor(Entidade, Subscription, opcoes);

    public ServiceBusReceiver CriarReceiver(ServiceBusClient cliente, ServiceBusReceiverOptions? opcoes = null) =>
        Subscription is null
            ? cliente.CreateReceiver(Entidade, opcoes ?? new())
            : cliente.CreateReceiver(Entidade, Subscription, opcoes ?? new());

    public override string ToString() => Subscription is null ? Entidade : $"{Entidade}/subscriptions/{Subscription}";
}
