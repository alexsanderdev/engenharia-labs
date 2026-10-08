-- 05 · EXISTS (semi-join)
-- Produtos que aparecem em pelo menos um item de pedido (qualquer status), SEM repetição.
-- Colunas: ProdutoId, Sku
-- Ordem:   ProdutoId
SELECT pr.Id AS ProdutoId,
       pr.Sku
FROM dbo.Produtos AS pr
WHERE EXISTS (SELECT 1 FROM dbo.ItensPedido AS i WHERE i.ProdutoId = pr.Id)
ORDER BY pr.Id;
