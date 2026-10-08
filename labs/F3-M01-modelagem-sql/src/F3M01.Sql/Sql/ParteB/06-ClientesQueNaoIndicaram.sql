-- 06 · NOT EXISTS vs NOT IN (NULL e lógica de três valores)
-- Clientes que NÃO indicaram ninguém (nenhum outro cliente tem IndicadoPorId = este cliente).
-- Atenção: IndicadoPorId tem NULLs.
-- Colunas: ClienteId, Nome
-- Ordem:   ClienteId
SELECT c.Id AS ClienteId,
       c.Nome
FROM dbo.Clientes AS c
WHERE NOT EXISTS (SELECT 1 FROM dbo.Clientes AS indicado WHERE indicado.IndicadoPorId = c.Id)
ORDER BY c.Id;
