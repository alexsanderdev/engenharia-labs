-- 01 · Tela "Meus pedidos" (NÃO ALTERE: o exercício é o índice em Indices.sql)
-- Parâmetro: @clienteId int
SELECT p.Id, p.CriadoEm, p.Status, p.Total
FROM dbo.Pedidos AS p
WHERE p.ClienteId = @clienteId
ORDER BY p.CriadoEm DESC;
