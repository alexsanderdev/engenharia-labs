-- 04 · Relatório de faturamento do mês (VOCÊ CORRIGE ESTA CONSULTA + cria o índice)
-- Parâmetros: @ano int, @mes int
-- Colunas: Pedidos, Faturamento (pedidos de qualquer status criados no mês)
-- TODO (Passo 5): o resultado está CERTO, mas a consulta não é SARGável.
--                 Reescreva o WHERE sem aplicar função sobre a coluna CriadoEm.
SELECT COUNT(*)     AS Pedidos,
       SUM(p.Total) AS Faturamento
FROM dbo.Pedidos AS p
WHERE YEAR(p.CriadoEm) = @ano
  AND MONTH(p.CriadoEm) = @mes;
