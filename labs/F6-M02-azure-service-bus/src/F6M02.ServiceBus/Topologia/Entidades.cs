namespace F6M02.ServiceBus.Topologia;

/// <summary>
/// Nomes das entidades do namespace. A topologia em si (propriedades, subscriptions e filtros)
/// está em <c>Topologia/Config.json</c>, o arquivo que o emulador lê ao subir. No Azure real, o
/// equivalente é Bicep/Terraform (ou o <c>ServiceBusAdministrationClient</c> num job de provisionamento).
/// </summary>
public static class Entidades
{
    /// <summary>Tópico onde o módulo de Pedidos publica <c>PedidoCriado</c>.</summary>
    public const string TopicoPedidos = "pedidos";

    /// <summary>Subscription sem filtro: recebe todos os pedidos (worker de notificações).</summary>
    public const string SubscriptionNotificacao = "notificacao";

    /// <summary>Subscription com SQL filter: só pedidos com valor total acima de R$ 500.</summary>
    public const string SubscriptionAntifraude = "antifraude";

    /// <summary>Subscription com correlation filter: só <c>PedidoCriado</c> de clientes VIP.</summary>
    public const string SubscriptionFidelidade = "fidelidade";

    /// <summary>Fila de processamento de pedidos (peek-lock, MaxDeliveryCount = 3).</summary>
    public const string FilaProcessamento = "pedidos-processamento";

    /// <summary>Fila com sessions (RequiresSession = true): eventos ordenados por pedido.</summary>
    public const string FilaEventosDoPedido = "pedidos-eventos";

    /// <summary>Fila com detecção de duplicatas (RequiresDuplicateDetection = true, janela de 1 min).</summary>
    public const string FilaPagamentos = "pagamentos";

    /// <summary>Fila para mensagens agendadas (lembretes de pagamento).</summary>
    public const string FilaLembretes = "lembretes";

    /// <summary>MaxDeliveryCount configurado em <see cref="FilaProcessamento"/> no Config.json.</summary>
    public const int MaxDeliveryCountProcessamento = 3;
}
