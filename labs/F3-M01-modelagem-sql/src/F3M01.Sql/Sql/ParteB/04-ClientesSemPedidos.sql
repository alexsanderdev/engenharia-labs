-- 04 · LEFT JOIN (anti-join)
-- Clientes que nunca fizeram nenhum pedido.
-- Colunas: ClienteId, Nome
-- Ordem:   ClienteId
SELECT c.Id AS ClienteId,
       c.Nome
FROM dbo.Clientes AS c
LEFT JOIN dbo.Pedidos AS p ON p.ClienteId = c.Id
WHERE p.Id IS NULL
ORDER BY c.Id;
