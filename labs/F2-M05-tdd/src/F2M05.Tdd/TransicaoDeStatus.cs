namespace F2M05.Tdd;

/// <summary>Uma entrada do histórico do pedido: de onde saiu, para onde foi, quando e (opcionalmente) por quê.</summary>
/// <param name="De">Status antes da transição.</param>
/// <param name="Para">Status depois da transição.</param>
/// <param name="Em">Instante da transição, lido do <see cref="TimeProvider"/>.</param>
/// <param name="Motivo">Motivo informado (obrigatório no cancelamento; nulo nas demais).</param>
public sealed record TransicaoDeStatus(StatusPedido De, StatusPedido Para, DateTimeOffset Em, string? Motivo = null);
