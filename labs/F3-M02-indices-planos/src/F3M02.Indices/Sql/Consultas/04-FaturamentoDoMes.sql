-- 04 · Relatório de faturamento do mês (VOCÊ CORRIGE ESTA CONSULTA + cria o índice)
-- Parâmetros: @ano int, @mes int
-- Colunas: Pedidos, Faturamento (pedidos de qualquer status criados no mês)
SELECT COUNT(*)     AS Pedidos,
       SUM(p.Total) AS Faturamento
FROM dbo.Pedidos AS p
WHERE p.CriadoEm >= DATEFROMPARTS(@ano, @mes, 1)                     -- coluna "pelada" dos dois lados:
  AND p.CriadoEm <  DATEADD(MONTH, 1, DATEFROMPARTS(@ano, @mes, 1));  -- intervalo SARGável
