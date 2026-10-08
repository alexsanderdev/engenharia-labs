namespace F1M01.CSharpModerno;

/// <summary>Status do pedido no OrderFlow.</summary>
public enum StatusPedido
{
    Criado,
    Confirmado,
    Concluido,
    Cancelado
}

/// <summary>Regras de transição de status, escritas com switch expressions e tuple patterns.</summary>
public static class TransicoesDePedido
{
    /// <summary>
    /// Transições válidas: Criado → Confirmado, Confirmado → Concluido, Criado → Cancelado.
    /// Qualquer outra (inclusive para o mesmo status) é inválida.
    /// </summary>
    public static bool PodeTransicionar(StatusPedido de, StatusPedido para) => (de, para) switch
    {
        (StatusPedido.Criado, StatusPedido.Confirmado) => true,
        (StatusPedido.Confirmado, StatusPedido.Concluido) => true,
        (StatusPedido.Criado, StatusPedido.Cancelado) => true,
        _ => false
    };

    /// <summary>
    /// Texto amigável: "Aguardando confirmação", "Em preparo", "Entregue", "Cancelado".
    /// Valor fora do enum lança <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    public static string Descrever(StatusPedido status) => status switch
    {
        StatusPedido.Criado => "Aguardando confirmação",
        StatusPedido.Confirmado => "Em preparo",
        StatusPedido.Concluido => "Entregue",
        StatusPedido.Cancelado => "Cancelado",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Status desconhecido.")
    };
}
