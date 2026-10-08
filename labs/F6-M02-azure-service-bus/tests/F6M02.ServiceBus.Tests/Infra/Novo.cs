using F6M02.ServiceBus.Contratos;

namespace F6M02.ServiceBus.Tests.Infra;

/// <summary>Dados de teste: cada chamada gera ids novos (testes não colidem entre si).</summary>
public static class Novo
{
    public static PedidoCriado Pedido(decimal valor = 150m, string segmento = "comum", string canal = "site") =>
        new(Guid.NewGuid(), Guid.NewGuid(), valor, segmento, canal, new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.Zero));

    public static string CorrelationId() => $"corr-{Guid.NewGuid():N}";
}
