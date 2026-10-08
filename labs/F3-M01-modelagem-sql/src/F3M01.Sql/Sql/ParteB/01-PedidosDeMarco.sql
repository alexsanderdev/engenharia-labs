-- 01 · JOIN + filtro de período
-- Pedidos criados em MARÇO/2026 (qualquer status), com o nome do cliente.
-- Colunas: PedidoId, Cliente, Status, Total
-- Ordem:   CriadoEm, PedidoId
SELECT p.Id     AS PedidoId,
       c.Nome   AS Cliente,
       p.Status,
       p.Total
FROM dbo.Pedidos AS p
JOIN dbo.Clientes AS c ON c.Id = p.ClienteId
WHERE p.CriadoEm >= '2026-03-01'
  AND p.CriadoEm <  '2026-04-01'   -- intervalo semiaberto: pega o dia 31 inteiro
ORDER BY p.CriadoEm, p.Id;
