-- 03 · GROUP BY + HAVING
-- Clientes com 3 ou mais pedidos NÃO cancelados, com o total gasto nesses pedidos.
-- Colunas: ClienteId, Nome, Pedidos, TotalGasto
-- Ordem:   TotalGasto DESC
SELECT c.Id          AS ClienteId,
       c.Nome,
       COUNT(*)      AS Pedidos,
       SUM(p.Total)  AS TotalGasto
FROM dbo.Clientes AS c
JOIN dbo.Pedidos  AS p ON p.ClienteId = c.Id
WHERE p.Status <> 'Cancelled'          -- filtro de LINHA: antes de agrupar
GROUP BY c.Id, c.Nome
HAVING COUNT(*) >= 3                   -- filtro de GRUPO: depois de agrupar
ORDER BY TotalGasto DESC;
