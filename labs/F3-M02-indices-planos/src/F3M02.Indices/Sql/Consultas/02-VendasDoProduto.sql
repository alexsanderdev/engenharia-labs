-- 02 · Card "vendas do produto" no painel do catálogo (NÃO ALTERE: o exercício é o índice)
-- Parâmetro: @produtoId int
SELECT COUNT(*)                            AS Itens,
       SUM(i.Quantidade)                   AS Unidades,
       SUM(i.Quantidade * i.PrecoUnitario) AS Receita
FROM dbo.ItensPedido AS i
WHERE i.ProdutoId = @produtoId;
