-- 02 · JOIN + GROUP BY
-- Por produto vendido: unidades e receita, considerando só pedidos NÃO cancelados.
-- Receita = soma de Quantidade * PrecoUnitario do ITEM (preço da época da compra).
-- Colunas: Sku, Produto, Unidades, Receita
-- Ordem:   Receita DESC, Sku
SELECT pr.Sku,
       pr.Nome                                  AS Produto,
       SUM(i.Quantidade)                        AS Unidades,
       SUM(i.Quantidade * i.PrecoUnitario)      AS Receita
FROM dbo.ItensPedido AS i
JOIN dbo.Pedidos  AS p  ON p.Id  = i.PedidoId
JOIN dbo.Produtos AS pr ON pr.Id = i.ProdutoId
WHERE p.Status <> 'Cancelled'
GROUP BY pr.Sku, pr.Nome
ORDER BY Receita DESC, pr.Sku;
