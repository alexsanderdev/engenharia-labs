-- 09 · SUM() OVER (total acumulado)
-- Para cada pedido NÃO cancelado: o total acumulado do cliente até aquele pedido,
-- em ordem de CriadoEm e, no empate, de PedidoId.
-- Colunas: ClienteId, PedidoId, Total, Acumulado
-- Ordem:   ClienteId, CriadoEm, PedidoId
SELECT p.ClienteId,
       p.Id AS PedidoId,
       p.Total,
       SUM(p.Total) OVER (PARTITION BY p.ClienteId
                          ORDER BY p.CriadoEm, p.Id
                          ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS Acumulado
FROM dbo.Pedidos AS p
WHERE p.Status <> 'Cancelled'
ORDER BY p.ClienteId, p.CriadoEm, p.Id;
