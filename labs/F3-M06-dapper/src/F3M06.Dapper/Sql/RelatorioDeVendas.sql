-- Relatório de vendas por dia (pedidos não cancelados) no período [@Inicio, @FimExclusivo).
-- Parâmetros: @Inicio (datetime2, meia-noite do 1º dia) e @FimExclusivo (meia-noite do dia seguinte ao último).
-- Colunas esperadas (alias = nome da propriedade em VendasPorDia): Dia, QuantidadePedidos, ItensVendidos, Faturamento.
--
-- Cuidados:
--  * Filtre a COLUNA crua (p.CriadoEm >= @Inicio AND p.CriadoEm < @FimExclusivo): sargável, usa o índice.
--    CAST(p.CriadoEm AS date) BETWEEN ... no WHERE esconde a coluna dentro de uma função.
--  * O JOIN com ItensPedido repete o pedido em cada item: SUM(p.Total) somaria o total várias vezes.
--    Some quantidade * preço dos ITENS e conte pedidos com COUNT(DISTINCT p.Id).
SELECT
    CAST(p.CriadoEm AS date)                 AS Dia,
    COUNT(DISTINCT p.Id)                     AS QuantidadePedidos,
    SUM(i.Quantidade)                        AS ItensVendidos,
    SUM(i.Quantidade * i.PrecoUnitario)      AS Faturamento
FROM Pedidos AS p
INNER JOIN ItensPedido AS i ON i.PedidoId = p.Id
WHERE p.CriadoEm >= @Inicio
  AND p.CriadoEm <  @FimExclusivo
  AND p.Status <> 'Cancelled'
GROUP BY CAST(p.CriadoEm AS date)
ORDER BY Dia;
