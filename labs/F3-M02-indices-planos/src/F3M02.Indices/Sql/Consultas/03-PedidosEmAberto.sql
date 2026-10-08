-- 03 · Fila de pedidos em aberto do backoffice (NÃO ALTERE: o exercício é o índice)
-- Os 50 pedidos 'Created' mais antigos. Literal (não parâmetro) de propósito: ver a Aula.
SELECT TOP (50) p.Id, p.ClienteId, p.CriadoEm, p.Total
FROM dbo.Pedidos AS p
WHERE p.Status = 'Created'
ORDER BY p.CriadoEm;
