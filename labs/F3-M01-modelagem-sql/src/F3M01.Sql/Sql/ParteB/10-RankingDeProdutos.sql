-- 10 · RANK / DENSE_RANK (ranking com empates)
-- Ranking de produtos por unidades vendidas (pedidos NÃO cancelados).
-- Devolva as 4 primeiras POSIÇÕES; produtos empatados dividem a posição (e todos aparecem).
-- Colunas: Posicao, Sku, Unidades
-- Ordem:   Posicao, Sku
WITH Vendas AS
(
    SELECT pr.Sku, SUM(i.Quantidade) AS Unidades
    FROM dbo.ItensPedido AS i
    JOIN dbo.Pedidos  AS p  ON p.Id  = i.PedidoId
    JOIN dbo.Produtos AS pr ON pr.Id = i.ProdutoId
    WHERE p.Status <> 'Cancelled'
    GROUP BY pr.Sku
),
Ranking AS
(
    SELECT DENSE_RANK() OVER (ORDER BY Unidades DESC) AS Posicao, Sku, Unidades
    FROM Vendas
)
SELECT Posicao, Sku, Unidades
FROM Ranking
WHERE Posicao <= 4
ORDER BY Posicao, Sku;
