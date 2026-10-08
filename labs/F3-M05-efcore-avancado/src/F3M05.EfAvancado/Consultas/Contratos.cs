namespace F3M05.EfAvancado.Consultas;

/// <summary>Linha do relatório de pedidos (tela de backoffice).</summary>
public sealed record PedidoResumo(int Id, string ClienteNome, int QuantidadeDeItens, decimal Total);

/// <summary>Linha do relatório de faturamento por dia (vem de SQL escrito à mão).</summary>
public sealed record FaturamentoDiario(DateOnly Dia, int Pedidos, decimal Total);
