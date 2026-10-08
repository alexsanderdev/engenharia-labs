-- 08 · ROW_NUMBER (top-1 por grupo)
-- Para cada cliente, o pedido NÃO cancelado de maior Total (empate: menor PedidoId).
-- Colunas: ClienteId, PedidoId, Total
-- Ordem:   ClienteId
WITH Ranqueados AS
(
    SELECT p.ClienteId,
           p.Id AS PedidoId,
           p.Total,
           ROW_NUMBER() OVER (PARTITION BY p.ClienteId ORDER BY p.Total DESC, p.Id) AS Posicao
    FROM dbo.Pedidos AS p
    WHERE p.Status <> 'Cancelled'
)
SELECT ClienteId, PedidoId, Total
FROM Ranqueados
WHERE Posicao = 1
ORDER BY ClienteId;
