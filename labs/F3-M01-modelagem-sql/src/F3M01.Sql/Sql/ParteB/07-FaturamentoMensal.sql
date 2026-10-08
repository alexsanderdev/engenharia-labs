-- 07 · CTE
-- Por mês (texto 'yyyy-MM'), considerando só pedidos NÃO cancelados:
-- quantidade de pedidos, faturamento e ticket médio arredondado a 2 casas.
-- Use uma CTE (WITH ... AS (...)).
-- Colunas: Mes, Pedidos, Faturamento, TicketMedio
-- Ordem:   Mes
WITH PedidosValidos AS
(
    SELECT CONVERT(char(7), p.CriadoEm, 126) AS Mes,   -- 126 = ISO 8601: 'yyyy-mm-ddThh:mi:ss'
           p.Total
    FROM dbo.Pedidos AS p
    WHERE p.Status <> 'Cancelled'
)
SELECT Mes,
       COUNT(*)                              AS Pedidos,
       SUM(Total)                            AS Faturamento,
       CAST(AVG(Total) AS decimal(18,2))     AS TicketMedio
FROM PedidosValidos
GROUP BY Mes
ORDER BY Mes;
