-- 05 · Busca de cliente por CPF no atendimento (VOCÊ CORRIGE ESTA CONSULTA; o índice único já existe)
-- Parâmetro: @documento — chega como NVARCHAR(11), igual ao AddWithValue("@documento", "123...") do ADO.NET.
-- Colunas: Id, Nome, Email
SELECT c.Id, c.Nome, c.Email
FROM dbo.Clientes AS c
WHERE c.Documento = CAST(@documento AS varchar(11));   -- converte o PARÂMETRO, não a coluna
